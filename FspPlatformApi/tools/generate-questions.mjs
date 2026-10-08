// Генератор банка вопросов через OpenAI-совместимый API (Gemini и др.).
// Пайплайн: generate → validate (tools/validate-questions.mjs) → insert в БД.
//
// Запуск (PowerShell):
//   $env:AI_BASE_URL="https://generativelanguage.googleapis.com/v1beta/openai"
//   $env:AI_API_KEY="AIzaSy..."
//   $env:AI_MODEL="gemini-3-flash"
//   node tools/generate-questions.mjs > seed-questions.sql
//
// Опционально: PAIRS="1:2,1:3" node tools/generate-questions.mjs   (только эти пары)
//              SLEEP_MS=800 node tools/generate-questions.mjs      (пауза между запросами)

const BASE_URL = (process.env.AI_BASE_URL ?? 'https://api.openai.com/v1').replace(/\/$/, '')
const API_KEY = process.env.AI_API_KEY
const MODEL = process.env.AI_MODEL ?? 'gemini-3-flash'
const SLEEP_MS = Number(process.env.SLEEP_MS ?? 500)

if (!API_KEY) {
  console.error('ERROR: задайте AI_API_KEY (и при необходимости AI_BASE_URL, AI_MODEL)')
  process.exit(1)
}

// ---------------------------------------------------------------------------
// Матрица «специализация × грейд». id — из справочника БД.
// Генерация платная по токенам: по умолчанию только ключевые пары,
// остальное покрыто базовым сидом. Добавляйте пары по мере необходимости.
// ---------------------------------------------------------------------------
const ALL_PAIRS = [
  { specId: 1, spec: 'Backend', grades: [{ id: 2, name: 'Джуниор' }, { id: 3, name: 'Миддл' }] },
  { specId: 2, spec: 'Frontend', grades: [{ id: 2, name: 'Джуниор' }, { id: 3, name: 'Миддл' }] },
  { specId: 4, spec: 'DevOps', grades: [{ id: 3, name: 'Миддл' }] },
  { specId: 5, spec: 'QA (тестирование)', grades: [{ id: 2, name: 'Джуниор' }] },
]

const PAIRS = process.env.PAIRS
  ? process.env.PAIRS.split(',').map((s) => {
      const [spec, grade] = s.split(':').map(Number)
      const p = ALL_PAIRS.find((x) => x.specId === spec)
      return { specId: spec, spec: p?.spec ?? `spec${spec}`, grades: p?.grades.filter((g) => g.id === grade) ?? [] }
    }).filter((p) => p.grades.length > 0)
  : ALL_PAIRS

const GRADE_FOCUS = {
  'Стажёр': 'абсолютные основы: синтаксис, базовые понятия, простейшие операции',
  'Джуниор': 'практические задачи ежедневной работы без глубокой архитектуры',
  'Миддл': 'самостоятельная работа: оптимизация, паттерны, отладка, проектирование модуля',
  'Сеньор': 'проектирование, трейдоффы, масштабирование, глубокое понимание платформы',
}

// Допустимые языки code-задач: python и javascript исполняются валидатором всегда;
// java/csharp — только если в системе есть javac/dotnet (иначе валидатор пропустит с warning).
const CODE_LANGUAGES = ['python', 'javascript', 'java', 'csharp']

function esc(s) {
  return String(s).replace(/'/g, "''")
}
function j(obj) {
  return `'${esc(JSON.stringify(obj))}'`
}
function uuid() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16)
  })
}

// ---------------------------------------------------------------------------
// Промпт: требования к вопросам (языконезависимость, изоморфные группы, рубрики)
// ---------------------------------------------------------------------------
function buildPrompt(spec, grade) {
  return `Ты — методолог по оценке ИТ-компетенций. Сгенерируй вопросы для теста "${spec}" на грейд "${grade}".
Фокус уровня: ${GRADE_FOCUS[grade] ?? 'профессиональные задачи'}.

Верни СТРОГО JSON (без текста вокруг):
{
  "questions": [
    {
      "group_index": 1,               // одинаковый для изоморфных вариантов одной компетенции (2-4 варианта)
      "question_type": "тест_с_вариантами" | "задача_с_кодом" | "открытый_вопрос",
      "difficulty": 1-5,              // суммарно по набору: 3×сложность 1-2, 4×сложность 2-3, 2×сложность 3-4, 1×сложность 4-5
      "prompt": "формулировка",
      "payload": { "options": ["a","b","c","d"] },                 // только тест_с_вариантами
      "payload": { "language": "python|javascript|java|csharp",
                   "reference": "ЭТАЛОННОЕ РЕШЕНИЕ — читает stdin, пишет stdout",
                   "testCases": [ { "input": "...", "expected": "..." } ] },  // только задача_с_кодом (4 тест-кейса)
      "payload": {},                                                 // только открытый_вопрос
      "correct_payload": { "correct": [индекс] },                    // тест_с_вариантами: ровно 1 правильный
      "correct_payload": { "rubric": "критерий1: слово1;слово2\\nкритерий2: слово3;слово4" },  // открытый_вопрос
      "correct_payload": {},                                         // задача_с_кодом
      "max_score": 1 | 3 | 2
    }
  ]
}

ЖЁСТКИЕ ПРАВИЛА:
1. Все вопросы ЯЗЫКОНЕЗАВИСИМЫЕ: без упоминания конкретного языка программирования, псевдокод/общие понятия. Исключение — задача_с_кодом: в ней явно указывается language.
2. Варианты одной группы (group_index) — ИЗОМОРФНЫЕ: та же компетенция, другие числа/формулировки, равная сложность.
3. У тест_с_вариантами ровно ОДИН правильный ответ, без двусмысленности.
4. Эталонное решение code-задачи ОБЯЗАНО проходить все 4 test-кейса. Ввод через stdin, вывод в stdout.
   Для javascript эталон начинается с кода чтения stdin: process.stdin.on('data'...) или readline.
5. Рубрика open-вопроса: 3-5 критериев, каждый с индикаторами через ';'.
6. Соотношение типов: 6 quiz : 2 code : 2 open. Всего 10 вопросов.
7. Не повторяй шаблонные вопросы про HTTP-коды и git — бери предметные темы: ${spec}.`
}

// ---------------------------------------------------------------------------
// Вызов API с повторными попытками
// ---------------------------------------------------------------------------
async function callApi(prompt) {
  for (let attempt = 1; attempt <= 3; attempt++) {
    try {
      const res = await fetch(`${BASE_URL}/chat/completions`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${API_KEY}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({
          model: MODEL,
          temperature: 0.7,
          response_format: { type: 'json_object' },
          messages: [
            { role: 'system', content: 'Отвечай только валидным JSON.' },
            { role: 'user', content: prompt },
          ],
        }),
      })
      if (!res.ok) throw new Error(`HTTP ${res.status}: ${(await res.text()).slice(0, 200)}`)
      const data = await res.json()
      const text = data.choices?.[0]?.message?.content ?? '{}'
      const parsed = JSON.parse(text)
      return parsed.questions ?? parsed
    } catch (e) {
      if (attempt === 3) throw e
      await sleep(2000 * attempt)
    }
  }
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms))
}

// ---------------------------------------------------------------------------
// Генерация и вывод SQL
// ---------------------------------------------------------------------------
console.log('-- ============================================================================')
console.log('-- АВТОГЕНЕРИРОВАННЫЙ БАНК ВОПРОСОВ. Перед вставкой ОБЯЗАТЕЛЬНО:')
console.log('--   node tools/validate-questions.mjs <этот файл>')
console.log('-- reference в payload code-задач — эталон для валидации, в БД не мешает.')
console.log(`-- Модель: ${MODEL} · ${new Date().toISOString()}`)
console.log('-- ============================================================================')
console.log()

let total = 0
for (const { specId, spec, grades } of PAIRS) {
  for (const grade of grades) {
    process.stderr.write(`Генерация: ${spec} / ${grade.name}... `)
    let questions
    try {
      questions = await callApi(buildPrompt(spec, grade.name))
    } catch (e) {
      process.stderr.write(`ОШИБКА: ${e.message}\n`)
      continue
    }
    process.stderr.write(`${questions.length} вопросов\n`)

    // Группируем по group_index → одинаковый question_group на семейство
    const groupUuids = new Map()
    for (const q of questions) {
      if (!groupUuids.has(q.group_index)) groupUuids.set(q.group_index, uuid())
      const type = q.question_type
      const payload = q.payload ?? {}
      // эталон reference не показываем в БД-полезной нагрузке? Оставляем — валидатор его читает.
      console.log(
        `INSERT INTO test_questions (specialization_id, grade_id, question_type, question_group, difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES ` +
          `(${specId}, ${grade.id}, '${esc(type)}', '${groupUuids.get(q.group_index)}', ${q.difficulty},\n` +
          ` ${j(q.prompt)},\n ${j(payload)},\n ${j(q.correct_payload ?? {})},\n ${q.max_score ?? (type === 'задача_с_кодом' ? 3 : type === 'открытый_вопрос' ? 2 : 1)}, true);`
      )
      total++
    }
    await sleep(SLEEP_MS)
  }
}
console.log()
console.log(`-- Итого сгенерировано вопросов: ${total}`)
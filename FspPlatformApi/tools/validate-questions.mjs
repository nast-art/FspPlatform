// Валидация сгенерированного банка вопросов ПЕРЕД вставкой в БД.
// Проверяет: корректность эталона quiz, рубрики open, и ПРОГОНЯЕТ эталонные решения
// code-задач по скрытым тест-кейсам на доступных языках.
//
// Поддерживаемые языки (по наличию тулчейна в системе):
//   python, javascript, typescript, java, csharp, c, cpp, go, php, ruby, rust, kotlin, swift
// Запуск: node tools/validate-questions.mjs seed-questions.sql [--json]
import { readFileSync, writeFileSync, mkdtempSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { spawnSync } from 'node:child_process'

const file = process.argv[2]
if (!file) { console.error('Usage: node tools/validate-questions.mjs <sql-file> [--json]'); process.exit(1) }
const asJson = process.argv.includes('--json')

const sql = readFileSync(file, 'utf8')
const errors = []
const warnings = []
let checked = 0

// ---------------------------------------------------------------------------
// Утилиты
// ---------------------------------------------------------------------------
function toolAvailable(cmd, args = ['--version']) {
  try {
    const r = spawnSync(cmd, args, { encoding: 'utf8', timeout: 5000 })
    return r.error === undefined
  } catch {
    return false
  }
}

/** Выполнить: записать исходник во временный файл, опционально скомпилировать, запустить с stdin. */
function compileAndRun({ prefix, source, compileCmd, runCmd, input, timeout = 8000 }) {
  const dir = mkdtempSync(join(tmpdir(), `val-${prefix}-`))
  const srcPath = join(dir, source.filename)
  writeFileSync(srcPath, source.content)
  if (compileCmd) {
    const c = spawnSync(compileCmd[0], compileCmd[1](srcPath, dir), { encoding: 'utf8', timeout: 15000 })
    if (c.status !== 0) return { status: 1, stdout: '', stderr: `compile: ${(c.stderr ?? '').slice(0, 200)}` }
  }
  const r = spawnSync(runCmd[0], runCmd[1](srcPath, dir), { input, encoding: 'utf8', timeout })
  return { status: r.status ?? 1, stdout: r.stdout ?? '', stderr: r.stderr ?? '' }
}

/** Интерпретатор/раннер «код целиком аргументом». */
function runInline(cmd, argsFn, code, input, timeout = 8000) {
  const r = spawnSync(cmd, argsFn(code), { input, encoding: 'utf8', timeout })
  return { status: r.status ?? 1, stdout: r.stdout ?? '', stderr: r.stderr ?? '' }
}

// ---------------------------------------------------------------------------
// Реестр языков: available проверяется один раз при старте
// ---------------------------------------------------------------------------
const RUNNERS = {
  python: {
    available: toolAvailable('python'),
    run: (code, input) => runInline('python', (c) => ['-c', c], code, input),
  },
  javascript: {
    available: toolAvailable('node'),
    run: (code, input) => runInline('node', (c) => ['-e', c], code, input),
  },
  typescript: {
    // требует ts-node (npx ts-node); без него — пропуск с warning
    available: toolAvailable('npx', ['ts-node', '--version']),
    run: (code, input) => {
      const dir = mkdtempSync(join(tmpdir(), 'val-ts-'))
      const p = join(dir, 'sol.ts')
      writeFileSync(p, code)
      const r = spawnSync('npx', ['ts-node', p], { input, encoding: 'utf8', timeout: 10000 })
      return { status: r.status ?? 1, stdout: r.stdout ?? '', stderr: r.stderr ?? '' }
    },
  },
  java: {
    available: toolAvailable('javac'),
    run: (code, input) => compileAndRun({
      prefix: 'java',
      source: { filename: 'Main.java', content: code.includes('class Main') ? code : `public class Main {\n${code}\n}` },
      compileCmd: ['javac', (src) => [src]],
      runCmd: ['java', (src, dir) => ['-cp', dir, 'Main']],
      input,
    }),
  },
  csharp: {
    available: toolAvailable('dotnet-script'),
    run: (code, input) => runInline('dotnet-script', (c) => ['eval', c], code, input, 10000),
  },
  c: {
    available: toolAvailable('gcc'),
    run: (code, input) => compileAndRun({
      prefix: 'c',
      source: { filename: 'main.c', content: code },
      compileCmd: ['gcc', (src, dir) => ['-O2', src, '-o', join(dir, 'main')]],
      runCmd: [null, (src, dir) => [join(dir, 'main')]],
      input,
    }),
  },
  cpp: {
    available: toolAvailable('g++'),
    run: (code, input) => compileAndRun({
      prefix: 'cpp',
      source: { filename: 'main.cpp', content: code },
      compileCmd: ['g++', (src, dir) => ['-O2', '-std=c++17', src, '-o', join(dir, 'main')]],
      runCmd: [null, (src, dir) => [join(dir, 'main')]],
      input,
    }),
  },
  go: {
    available: toolAvailable('go'),
    run: (code, input) => compileAndRun({
      prefix: 'go',
      source: { filename: 'main.go', content: code },
      compileCmd: null,
      runCmd: ['go', (src) => ['run', src]],
      input,
      timeout: 15000,
    }),
  },
  php: {
    available: toolAvailable('php'),
    run: (code, input) => compileAndRun({
      prefix: 'php',
      source: { filename: 'solution.php', content: code.startsWith('<?php') ? code : `<?php\n${code}\n?>` },
      compileCmd: null,
      runCmd: ['php', (src) => [src]],
      input,
    }),
  },
  ruby: {
    available: toolAvailable('ruby'),
    run: (code, input) => compileAndRun({
      prefix: 'ruby',
      source: { filename: 'solution.rb', content: code },
      compileCmd: null,
      runCmd: ['ruby', (src) => [src]],
      input,
    }),
  },
  rust: {
    available: toolAvailable('rustc'),
    run: (code, input) => compileAndRun({
      prefix: 'rust',
      source: { filename: 'main.rs', content: code },
      compileCmd: ['rustc', (src, dir) => ['-O', src, '-o', join(dir, 'main')]],
      runCmd: [null, (src, dir) => [join(dir, 'main')]],
      input,
      timeout: 15000,
    }),
  },
  kotlin: {
    available: toolAvailable('kotlinc'),
    run: (code, input) => compileAndRun({
      prefix: 'kotlin',
      source: {
        filename: 'Main.kt',
        content: code.includes('fun main') ? code : `fun main() {\n${code}\n}`,
      },
      compileCmd: ['kotlinc', (src, dir) => [src, '-include-runtime', '-d', join(dir, 'main.jar')]],
      runCmd: ['java', (src, dir) => ['-jar', join(dir, 'main.jar')]],
      input,
      timeout: 15000,
    }),
  },
  swift: {
    available: toolAvailable('swift'),
    run: (code, input) => compileAndRun({
      prefix: 'swift',
      source: { filename: 'main.swift', content: code },
      compileCmd: null,
      runCmd: ['swift', (src) => [src]],
      input,
      timeout: 15000,
    }),
  },
}

// ---------------------------------------------------------------------------
// 1. Разбор INSERT'ов test_questions (payload/correct_payload как JSON)
// ---------------------------------------------------------------------------
const rowRe = /'(?:[^']|'')*',\s*'(\{(?:[^']|'')*\})',\s*'(\{(?:[^']|'')*\})',\s*(\d+(?:\.\d+)?)/g
let m
while ((m = rowRe.exec(sql)) !== null) {
  checked++
  let payload, correct
  try {
    payload = JSON.parse(m[1].replace(/''/g, "'"))
    correct = JSON.parse(m[2].replace(/''/g, "'"))
  } catch {
    errors.push(`строка ${checked}: невалидный JSON payload/correct`)
    continue
  }
  validateRow(checked, payload, correct, parseFloat(m[3]))
}

// ---------------------------------------------------------------------------
// 2. code-задачи: прогон эталона по тест-кейсам на доступном языке
// ---------------------------------------------------------------------------
const codeBlocks = [...sql.matchAll(/'задача_с_кодом'[\s\S]*?'(\{[^{}]*"testCases"[\s\S]*?\})'/g)]
for (const [i, b] of codeBlocks.entries()) {
  let payload
  try {
    payload = JSON.parse(b[1].replace(/''/g, "'"))
  } catch {
    errors.push(`code-блок ${i + 1}: не удалось разобрать payload`)
    continue
  }
  const lang = String(payload.language ?? 'python').toLowerCase()
  const ref = payload.reference
  if (!ref) {
    warnings.push(`code-блок ${i + 1}: нет эталонного решения (payload.reference) — прогон пропущен`)
    continue
  }
  const runner = RUNNERS[lang]
  if (!runner) {
    warnings.push(`code-блок ${i + 1}: язык "${lang}" не поддерживается валидатором — прогон пропущен`)
    continue
  }
  if (!runner.available) {
    warnings.push(`code-блок ${i + 1}: язык "${lang}" — нет тулчейна в системе — прогон пропущен (проверьте вручную)`)
    continue
  }
  for (const tc of payload.testCases ?? []) {
    const r = runner.run(ref, tc.input ?? '')
    const norm = (s) => (s ?? '').replace(/\r\n/g, '\n').trimEnd()
    if (r.status !== 0) {
      errors.push(`code-блок ${i + 1} (${lang}): эталон упал на входе "${tc.input}": ${r.stderr.slice(0, 150)}`)
    } else if (norm(r.stdout) !== norm(tc.expected)) {
      errors.push(
        `code-блок ${i + 1} (${lang}): эталон даёт "${norm(r.stdout)}", ожидается "${norm(tc.expected)}" (вход "${tc.input}")`
      )
    }
  }
}

// ---------------------------------------------------------------------------
// Отчёт
// ---------------------------------------------------------------------------
const report = { checked, errors, warnings }
if (asJson) console.log(JSON.stringify(report, null, 2))
else {
  console.log(`Проверено строк: ${checked} · code-блоков: ${codeBlocks.length}`)
  warnings.forEach((w) => console.log(`⚠ ${w}`))
  if (errors.length === 0) console.log('✓ Ошибок нет — можно вставлять в БД')
  else {
    errors.forEach((e) => console.log(`✗ ${e}`))
    process.exit(1)
  }
}

// ---------------------------------------------------------------------------
// Проверки одной строки
// ---------------------------------------------------------------------------
function validateRow(n, payload, correct, maxScore) {
  if (payload.options && correct.correct) {
    const opts = payload.options
    const corr = correct.correct
    if (corr.length !== 1)
      errors.push(`строка ${n}: у quiz должен быть ровно 1 правильный вариант, найдено ${corr.length}`)
    for (const i of corr)
      if (i < 0 || i >= opts.length)
        errors.push(`строка ${n}: индекс правильного ответа ${i} вне диапазона 0..${opts.length - 1}`)
    const lower = opts.map((o) => String(o).toLowerCase().trim())
    if (new Set(lower).size !== lower.length)
      errors.push(`строка ${n}: есть дублирующиеся варианты ответа`)
  }
  if (correct.rubric !== undefined) {
    const criteria = String(correct.rubric).split('\n').map((s) => s.trim()).filter(Boolean)
    if (criteria.length < 2) warnings.push(`строка ${n}: рубрика содержит <2 критериев — проверьте вручную`)
    for (const c of criteria)
      if (!c.includes(';')) warnings.push(`строка ${n}: критерий без индикаторов через ";": "${c.slice(0, 50)}"`)
  }
  if (!(maxScore > 0)) errors.push(`строка ${n}: max_score должен быть > 0`)
}
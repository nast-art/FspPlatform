import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { testsApi } from '../../api/tests.api'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'

// Тип вопроса определяем по payload (DTO отдаёт русское имя типа)
function questionKind(q) {
  const p = q.payload
  if (p?.options) return 'quiz'
  if (p?.testCases) return 'code'
  return 'open'
}

function fmtTimer(ms) {
  const s = Math.max(0, Math.floor(ms / 1000))
  const m = Math.floor(s / 60)
  return `${String(m).padStart(2, '0')}:${String(s % 60).padStart(2, '0')}`
}

// Шаблоны под каждый язык (бэкенд-раннер: python, javascript, java, csharp, go, cpp)
const TEMPLATES = {
  python:
    'import sys\n\nfor line in sys.stdin:\n    pass\n# ваш код здесь',
  javascript:
    'let data = "";\nprocess.stdin.on("data", (c) => (data += c));\nprocess.stdin.on("end", () => {\n  data = data.trim();\n  // ваш код здесь\n});',
  java:
    'import java.util.*;\n\npublic class Solution {\n    public static void main(String[] args) {\n        Scanner sc = new Scanner(System.in);\n        // ваш код здесь\n    }\n}',
  csharp:
    'using System;\n\npublic class Solution {\n    public static void Main() {\n        string data = Console.In.ReadToEnd();\n        // ваш код здесь\n    }\n}',
  go:
    'package main\n\nimport (\n    "bufio"\n    "fmt"\n    "os"\n)\n\nfunc main() {\n    in := bufio.NewReader(os.Stdin)\n    _ = in\n    // ваш код здесь\n}',
  cpp:
    '#include <bits/stdc++.h>\n\nint main() {\n    std::ios::sync_with_stdio(false);\n    std::cin.tie(nullptr);\n    // чтение из std::cin, вывод в std::cout\n    return 0;\n}',
};

const LANG_LABEL = {
  python: "Python 3",
  javascript: "JavaScript (Node 20)",
  java: "Java 17",
  csharp: "C# (Mono 6)",
  go: "Go 1.22",
  cpp: "C++17 (GCC 13)",
};

export default function TestSessionPage() {
  const { sessionId } = useParams()
  const navigate = useNavigate()

  const [session, setSession] = useState(null)
  // answers: questionId → { selected } | { code, language } | { text }
  const [answers, setAnswers] = useState({})
  const [current, setCurrent] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [result, setResult] = useState(null)
  const [submitting, setSubmitting] = useState(false)
  const [now, setNow] = useState(Date.now())
  const finishedRef = useRef(false)

  useEffect(() => {
    testsApi
      .getSession(sessionId)
      .then(setSession)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }, [sessionId])

  useEffect(() => {
    if (!session) return
    const t = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(t)
  }, [session])

  const leftMs = useMemo(
    () => (session ? new Date(session.expiresAt).getTime() - now : 0),
    [session, now]
  )

  // Авто-завершение по истечении времени
  useEffect(() => {
    if (session && leftMs <= 0 && !finishedRef.current) {
      finishedRef.current = true
      doFinish()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [leftMs, session])

  function setAnswer(questionId, patch) {
    setAnswers((a) => ({ ...a, [questionId]: patch }))
  }

  async function doFinish() {
    if (finishedRef.current && result) return
    finishedRef.current = true
    setSubmitting(true)
    setError(null)
    try {
      // Все отвеченные вопросы — одним пакетом (language уходит для code-задач)
      const payload = Object.entries(answers).map(([questionId, a]) => ({
        questionId,
        selected: a.selected,
        code: a.code,
        language: a.language,
        text: a.text,
      }))
      if (payload.length > 0) {
        await testsApi.submitAnswers(sessionId, { answers: payload })
      }
      const res = await testsApi.finish(sessionId)
      setResult(res)
    } catch (e) {
      setError(e.message)
      finishedRef.current = false
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <Spinner text="Собираем ваш уникальный набор заданий…" />

  // Экран результата
  if (result) {
    return (
      <div style={{ maxWidth: 560, margin: '40px auto' }}>
        <div className="card" style={{ textAlign: 'center' }}>
          <div className="h1" style={{ fontSize: 18 }}>
            {result.passed === true ? 'Тест сдан' : 'Тест не пройден'}
          </div>
          <p className="sub">
            {result.percent != null ? `Результат: ${result.percent}%` : ''}
            {result.resultingGradeName
              ? ` · грейд «${result.resultingGradeName}»`
              : ''}
          </p>
          <div className="notice" style={{ textAlign: 'left', marginBottom: 16 }}>
            {result.message}
          </div>
          <div className="row" style={{ justifyContent: 'center' }}>
            <button
              className="btn"
              onClick={() => navigate('/candidate/category', { replace: true })}
            >
              Моя категория
            </button>
            <button className="btn ghost" onClick={() => navigate('/candidate')}>
              В кабинет
            </button>
          </div>
        </div>
      </div>
    )
  }

  if (error && !session)
    return (
      <div style={{ maxWidth: 560, margin: '40px auto' }}>
        <div className="notice red">{error}</div>
        <button
          className="btn ghost"
          style={{ marginTop: 14 }}
          onClick={() => navigate('/candidate/category')}
        >
          Назад
        </button>
      </div>
    )

  const q = session.questions[current]
  const kind = questionKind(q)
  const answeredCount = session.questions.filter(
    (qq) => answers[qq.questionId] != null
  ).length
  const timeDanger = leftMs < 5 * 60 * 1000

  // Язык code-ответа: сохранённый или дефолтный python
  const curLang = answers[q.questionId]?.language ?? 'python'
  // Шаблон: сначала сохранённый код кандидата, иначе шаблон под выбранный язык
  const curCode =
    answers[q.questionId]?.code ?? TEMPLATES[curLang]

  return (
    <div className="test-layout">
      <div className="content">
        <div className="row" style={{ justifyContent: 'space-between' }}>
          <div className="row" style={{ gap: 8 }}>
            <Badge color="blue">
              Вопрос {current + 1} из {session.questions.length}
            </Badge>
            <Badge>{session.title}</Badge>
          </div>
          <span className={`badge red ${timeDanger ? 'timer-danger' : ''}`}>
            <span className="d" />
            {fmtTimer(leftMs)}
          </span>
        </div>

        <div className="progress" style={{ margin: '16px 0 22px' }}>
          <i
            style={{ width: `${(answeredCount / session.questions.length) * 100}%` }}
          />
        </div>

        {error && (
          <div className="notice red" style={{ marginBottom: 14 }}>
            {error}
          </div>
        )}

        <div className="card">
          <div className="mono" style={{ marginBottom: 8 }}>
            {
              {
                quiz: 'тест с вариантами ответа',
                code: 'задача с кодом',
                open: 'открытый вопрос',
              }[kind]
            }
          </div>
          <p style={{ fontSize: 15, marginBottom: 14 }}>{q.prompt}</p>

          {kind === 'quiz' && (
            <div>
              {q.payload.options.map((opt, idx) => {
                const sel = answers[q.questionId]?.selected === idx
                return (
                  <div
                    key={idx}
                    className={`q-opt ${sel ? 'sel' : ''}`}
                    onClick={() => setAnswer(q.questionId, { selected: idx })}
                  >
                    <span className="k">{idx + 1}</span>
                    {opt}
                  </div>
                )
              })}
            </div>
          )}

          {kind === 'code' && (
            <div>
              <div className="row" style={{ gap: 8, marginBottom: 10 }}>
                <span className="mono">Язык:</span>
                <select
                  className="input"
                  style={{ width: 'auto', minWidth: 220, height: 36 }}
                  value={curLang}
                  onChange={(e) => {
                    const lang = e.target.value
                    // Смена языка → подставляем шаблон под язык (сохранённый код теряем, это осознанно)
                    setAnswer(q.questionId, { code: TEMPLATES[lang], language: lang })
                  }}
                >
                  <option value="python">{LANG_LABEL.python}</option>
                  <option value="javascript">{LANG_LABEL.javascript}</option>
                  <option value="java">{LANG_LABEL.java}</option>
                  <option value="csharp">{LANG_LABEL.csharp}</option>
                  <option value="go">{LANG_LABEL.go}</option>
                  <option value="cpp">{LANG_LABEL.cpp}</option>
                  
                </select>
              </div>
              <textarea
                className="input"
                rows={10}
                style={{ fontFamily: 'var(--font)', fontSize: 12.5 }}
                value={curCode}
                onChange={(e) =>
                  setAnswer(q.questionId, {
                    code: e.target.value,
                    language: curLang,
                  })
                }
              />
              <p className="hint" style={{ margin: '8px 0' }}>
                Решение проверяется автоматически на скрытых тест-кейсах в
                изолированной среде. Ввод подаётся через stdin — в шаблоне это
                уже учтено.
              </p>
            </div>
          )}

          {kind === 'open' && (
            <textarea
              className="input"
              rows={6}
              placeholder="Опишите ваш ответ…"
              value={answers[q.questionId]?.text ?? ''}
              onChange={(e) => setAnswer(q.questionId, { text: e.target.value })}
            />
          )}

          <div className="row" style={{ marginTop: 14, gap: 10 }}>
            <button
              className="btn ghost sm"
              disabled={current === 0}
              onClick={() => setCurrent((c) => c - 1)}
            >
              Назад
            </button>
            {current < session.questions.length - 1 ? (
              <button className="btn sm" onClick={() => setCurrent((c) => c + 1)}>
                Далее
              </button>
            ) : (
              <button className="btn sm" onClick={doFinish} disabled={submitting}>
                {submitting ? 'Проверка…' : 'Завершить тест'}
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Лента вопросов */}
      <div className="test-tape">
        <div className="mono" style={{ marginBottom: 12 }}>ЛЕНТА ВОПРОСОВ</div>
        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          {session.questions.map((qq, idx) => (
            <span
              key={qq.questionId}
              className={`tag ${answers[qq.questionId] != null ? 'sel' : ''} ${
                idx === current ? 'current' : ''
              }`}
              style={{ cursor: 'pointer' }}
              onClick={() => setCurrent(idx)}
            >
              {idx + 1}
            </span>
          ))}
        </div>
        <div className="divider" />
        <p className="hint">
          Отвечено {answeredCount} из {session.questions.length}. У каждого
          кандидата — свой набор заданий из банка, поэтому пересылка вопросов
          не даёт преимущества.
        </p>
        <button
          className="btn"
          style={{ width: '100%', marginTop: 14 }}
          onClick={doFinish}
          disabled={submitting}
        >
          {submitting ? 'Проверка…' : 'Завершить тест'}
        </button>
      </div>
    </div>
  )
}
-- Идемпотентная подготовка тестирования для всех грейдов.
-- Выполняется сервисом db-bootstrap и на новом, и на уже существующем volume.
-- Для пары «специализация + грейд» без собственного активного банка берётся ближайший
-- доступный банк этой специализации. Это стартовое наполнение стенда; вопросы можно
-- заменить из админки после подготовки полноценного банка.

BEGIN;

-- 1. Заполняем недостающие уровни вопросов для специализаций, у которых уже есть банк.
-- Копируем вопросную группу как отдельную группу и сдвигаем сложность к целевому рангу.
WITH missing_pairs AS MATERIALIZED (
    SELECT s.id AS specialization_id,
           target.id AS target_grade_id,
           target.rank AS target_rank,
           source.source_grade_id,
           source.source_rank
    FROM specializations s
    CROSS JOIN grades target
    JOIN LATERAL (
        SELECT q.grade_id AS source_grade_id,
               source_grade.rank AS source_rank
        FROM test_questions q
        JOIN grades source_grade ON source_grade.id = q.grade_id
        WHERE q.specialization_id = s.id
          AND q.is_active
          AND q.grade_id <> target.id
        GROUP BY q.grade_id, source_grade.rank
        ORDER BY ABS(source_grade.rank - target.rank), source_grade.rank
        LIMIT 1
    ) source ON TRUE
    WHERE target.is_active
      AND NOT EXISTS (
          SELECT 1
          FROM test_questions existing
          WHERE existing.specialization_id = s.id
            AND existing.grade_id = target.id
            AND existing.is_active
      )
), source_groups AS MATERIALIZED (
    SELECT DISTINCT p.specialization_id, p.target_grade_id,
           p.target_rank, p.source_grade_id, p.source_rank,
           q.question_group AS source_group
    FROM missing_pairs p
    JOIN test_questions q
      ON q.specialization_id = p.specialization_id
     AND q.grade_id = p.source_grade_id
     AND q.is_active
), group_map AS MATERIALIZED (
    SELECT specialization_id, target_grade_id, source_grade_id, source_group,
           gen_random_uuid() AS target_group
    FROM source_groups
)
INSERT INTO test_questions (
    id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active
)
SELECT gen_random_uuid(),
       q.specialization_id,
       p.target_grade_id,
       q.question_type,
       gm.target_group,
       GREATEST(1, LEAST(5, q.difficulty + p.target_rank - p.source_rank))::smallint,
       q.prompt,
       q.payload,
       q.correct_payload,
       q.max_score,
       TRUE
FROM missing_pairs p
JOIN test_questions q
  ON q.specialization_id = p.specialization_id
 AND q.grade_id = p.source_grade_id
 AND q.is_active
JOIN group_map gm
  ON gm.specialization_id = p.specialization_id
 AND gm.target_grade_id = p.target_grade_id
 AND gm.source_grade_id = p.source_grade_id
 AND gm.source_group = q.question_group;

-- 2. Если у активного банка нет шаблона на соответствующий грейд, создаём его.
-- Сначала используем настройки ближайшего настроенного теста той же специализации.
WITH question_counts AS MATERIALIZED (
    SELECT specialization_id, grade_id, COUNT(*)::integer AS active_count
    FROM test_questions
    WHERE is_active
    GROUP BY specialization_id, grade_id
), missing_assignments AS MATERIALIZED (
    SELECT qc.specialization_id,
           qc.grade_id,
           qc.active_count,
           s.name AS specialization_name,
           g.name AS grade_name,
           COALESCE(src.duration_minutes, 30)::smallint AS duration_minutes,
           COALESCE(src.pass_threshold, 60.00)::numeric(5,2) AS pass_threshold,
           LEAST(COALESCE(src.questions_count, 10), qc.active_count, 10)::smallint AS questions_count,
           COALESCE(src.difficulty_mix, '{"1":1,"2":3,"3":4,"4":2}'::jsonb) AS difficulty_mix,
           COALESCE((
               SELECT MAX(existing.version) + 1
               FROM test_assignments existing
               WHERE existing.specialization_id = qc.specialization_id
                 AND existing.grade_id = qc.grade_id
           ), 1) AS new_version
    FROM question_counts qc
    JOIN specializations s ON s.id = qc.specialization_id
    JOIN grades g ON g.id = qc.grade_id
    LEFT JOIN LATERAL (
        SELECT a.duration_minutes, a.pass_threshold, a.questions_count, a.difficulty_mix
        FROM test_assignments a
        JOIN grades source_grade ON source_grade.id = a.grade_id
        WHERE a.specialization_id = qc.specialization_id
          AND a.is_active
          AND a.grade_id <> qc.grade_id
        ORDER BY ABS(source_grade.rank - g.rank), source_grade.rank
        LIMIT 1
    ) src ON TRUE
    WHERE NOT EXISTS (
        SELECT 1 FROM test_assignments a
        WHERE a.specialization_id = qc.specialization_id
          AND a.grade_id = qc.grade_id
          AND a.is_active
    )
)
INSERT INTO test_assignments (
    id, specialization_id, grade_id, title, description,
    duration_minutes, pass_threshold, questions_count,
    difficulty_mix, version, is_active
)
SELECT gen_random_uuid(),
       specialization_id,
       grade_id,
       specialization_name || ' — ' || grade_name,
       'Автоматический шаблон для имеющегося банка вопросов; замените начальный набор экспертными вопросами при подготовке продуктивного стенда.',
       duration_minutes,
       pass_threshold,
       questions_count,
       difficulty_mix,
       new_version,
       TRUE
FROM missing_assignments
WHERE active_count > 0
ON CONFLICT (specialization_id, grade_id, version) DO NOTHING;

COMMIT;

-- Контроль заполнения: выводим пары специализация + грейд, по которым есть шаблон и вопросы.
SELECT s.name AS specialization,
       g.name AS grade,
       COUNT(DISTINCT a.id) FILTER (WHERE a.is_active) AS active_assignments,
       COUNT(q.id) FILTER (WHERE q.is_active) AS active_questions
FROM specializations s
CROSS JOIN grades g
LEFT JOIN test_assignments a
  ON a.specialization_id = s.id AND a.grade_id = g.id
LEFT JOIN test_questions q
  ON q.specialization_id = s.id AND q.grade_id = g.id
GROUP BY s.name, g.name, g.rank
ORDER BY s.name, g.rank;

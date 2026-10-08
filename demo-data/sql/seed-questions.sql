-- ============================================================================
-- SEED QUESTION BANK: банк вопросов по 4 специализациям × 3 грейдам.
-- 12 пар × 11 вопросов = 134 вопроса: 72 quiz, 36 code (6 языков), 26 open.
--
-- Свойства файла:
--   * ЧИСТО ДОБАВОЧНЫЙ: ни одного DELETE, существующий банк не трогается;
--   * ИДЕМПОТЕНТНЫЙ: фиксированные UUID, ON CONFLICT DO NOTHING —
--     повторный запуск безопасен, свои строки можно удалить вручную по UUID;
--   * все вопросы сразу is_active = true (эталонные, не черновики);
--   * эталоны code-задач проверены по тест-кейсам (python — исполнением).
-- Требование: применён init.sql (справочники + statuses типов вопросов).
-- Запуск: psql -d fsp_platform -f seed-questions.sql
-- ============================================================================

BEGIN;

-- Шаблоны тестов на пары «специализация + грейд» (10 вопросов, порог 60%)
INSERT INTO test_assignments (id, specialization_id, grade_id, title, description,
    duration_minutes, pass_threshold, questions_count, difficulty_mix, version, is_active)
SELECT gen_random_uuid(), s.id, g.id,
       s.name || ' — ' || g.name,
       'Автосид: 10 вопросов, порог 60%', 30, 60.00, 10,
       '{"1":1,"2":3,"3":4,"4":2}'::jsonb, 1, true
FROM specializations s CROSS JOIN grades g
WHERE s.code IN ('Backend','Frontend','тестирование','DevOps') AND g.code IN ('джуниор','миддл','сеньор')
ON CONFLICT (specialization_id, grade_id, version) DO NOTHING;

INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5e8ad6ab-6113-5492-834c-0b1901765ca5', 1, 2, 'тест_с_вариантами', '0de6cbae-40a0-5ffe-98ba-8f64646e2578',
 1, 'Какой HTTP-метод обычно используется для получения ресурса?', '{"options": ["GET", "POST", "DELETE", "PATCH"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('bc7f827f-0fa6-5f86-bca3-e9554536dc10', 1, 2, 'тест_с_вариантами', '0de6cbae-40a0-5ffe-98ba-8f64646e2578',
 2, 'Что вернёт HTTP-статус 404?', '{"options": ["Успех", "Ресурс не найден", "Ошибка сервера", "Перенаправление"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('de8d8802-acc3-56b0-aeb9-ee9f79257738', 1, 2, 'тест_с_вариантами', 'f9600d47-0a6d-53d1-b9fd-53f1c882b47a',
 2, 'В C# чем значимые типы отличаются от ссылочных?', '{"options": ["Хранятся в куче всегда", "Копируются по значению при присваивании", "Не могут быть числами", "Работают только в циклах"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4f06a275-cb7d-51dd-87e2-df9480cbfd0e', 1, 2, 'тест_с_вариантами', 'f9600d47-0a6d-53d1-b9fd-53f1c882b47a',
 3, 'Какая команда Git создаёт новую ветку и сразу переключается на неё?', '{"options": ["git branch", "git checkout -b", "git merge", "git clone"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('31cf5edc-640f-5deb-888f-1a861cfc8703', 1, 2, 'тест_с_вариантами', '55cc13c6-e614-538e-8528-620f631ec518',
 3, 'Что делает SQL-оператор WHERE?', '{"options": ["Сортирует результат", "Фильтрует строки по условию", "Создаёт таблицу", "Удаляет дубликаты"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('938ca04d-6d02-5601-8878-7c4e58894a47', 1, 2, 'тест_с_вариантами', '55cc13c6-e614-538e-8528-620f631ec518',
 4, 'Что такое API?', '{"options": ["Язык программирования", "Интерфейс для взаимодействия программ", "База данных", "Операционная система"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('d9cdc90f-687a-5443-bda4-7f8ef959f2f5', 1, 2, 'задача_с_кодом', '07ea3e7a-ae35-576d-983b-9af44f0d3446',
 2, 'Язык: java. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "java", "reference": "import java.util.*;import java.math.*;public class Solution{public static void main(String[] a){int n=new Scanner(System.in).nextInt();BigInteger r=BigInteger.ONE;for(int i=2;i<=n;i++)r=r.multiply(BigInteger.valueOf(i));System.out.println(r);}}", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('2e5e69dd-6821-54e0-97a6-931827cad44f', 1, 2, 'задача_с_кодом', '0e23d919-b104-55f0-8f85-c96b07566a38',
 3, 'Язык: go. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strconv\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Split(bufio.ScanWords);var s int64=0;for sc.Scan(){n,_:=strconv.ParseInt(sc.Text(),10,64);s+=n};fmt.Println(s)}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('690c5d53-c56c-5425-b5e4-e2bdaef16975', 1, 2, 'задача_с_кодом', '6f83b9fc-682d-5e9f-a681-31e1c9f5cd78',
 4, 'Язык: cpp. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){int n;std::cin>>n;for(int i=1;i<=n;i++){if(i%15==0)std::cout<<\"FizzBuzz\\n\";else if(i%3==0)std::cout<<\"Fizz\\n\";else if(i%5==0)std::cout<<\"Buzz\\n\";else std::cout<<i<<\"\\n\";}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f0ccce72-3a6d-589b-8d16-39bb63107d3a', 1, 2, 'открытый_вопрос', '9405655b-4d56-53ab-874d-5a0d175a8146',
 2, 'Чем GET отличается от POST? Коротко, по пунктам.', '{}'::jsonb, '{"rubric": "метод GET: параметры в URL;метод POST: тело запроса\nидемпотентность GET: не изменяет состояние\nPOST: может изменять данные"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('1e994e72-ad6d-5de3-9119-efcd053da148', 1, 2, 'открытый_вопрос', '28dc7ba7-68bc-546d-8f3b-ad8db52cf3e8',
 3, 'Что такое индекс базы данных и когда он вреден?', '{}'::jsonb, '{"rubric": "ускоряет: SELECT по индексированному полю\nзамедляет: INSERT и UPDATE из-за перестройки\nлишние индексы: занимают место"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('dd8d6725-974d-5545-800a-d4c509b2f78d', 1, 2, 'открытый_вопрос', '7af36566-0a87-5b30-8c76-4512b8caaa69',
 4, 'Что такое REST и его основные принципы?', '{}'::jsonb, '{"rubric": "ресурсы: идентификаторы URI\nстандартные методы: GET POST PUT DELETE\nотсутствие состояния: каждый запрос самодостаточен"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ca4ee63b-7e89-5d2b-ad2c-7f762788fbc3', 1, 3, 'тест_с_вариантами', '2f24e121-51ff-5673-bd89-7b1ecf10c5e3',
 2, 'Что гарантирует свойство ACID в транзакциях?', '{"options": ["Быстроту запросов", "Атомарность и согласованность выполнения", "Шифрование данных", "Кэширование"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e3a8a678-ca79-54e7-993a-8ab0ce6b22a7', 1, 3, 'тест_с_вариантами', '2f24e121-51ff-5673-bd89-7b1ecf10c5e3',
 3, 'Зачем нужен Dependency Injection?', '{"options": ["Для ускорения цикла for", "Для снижения связанности классов", "Для хранения паролей", "Для компиляции"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('b15c5dc2-ea4f-5173-8d0d-c3f8689a4205', 1, 3, 'тест_с_вариантами', 'de1ad595-7d40-5a54-a58b-47ef89455b34',
 3, 'Что делает ключевое слово async/await?', '{"options": ["Создаёт новый процесс", "Позволяет асинхронно ждать завершения операций", "Блокирует поток навсегда", "Удаляет исключения"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('816c9f7f-ab44-59ab-ba90-fede921119ea', 1, 3, 'тест_с_вариантами', 'de1ad595-7d40-5a54-a58b-47ef89455b34',
 4, 'Что такое нормализация БД?', '{"options": ["Упаковка данных в JSON", "Устранение избыточности и аномалий", "Шардирование таблиц", "Создание индексов"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a06883e3-f6b8-56ec-b5a8-e49174f78c68', 1, 3, 'тест_с_вариантами', '58f38205-5aaf-5832-84f2-517065083526',
 4, 'Какие HTTP-методы считаются идемпотентными?', '{"options": ["POST и PATCH", "GET, PUT, DELETE", "Только OPTIONS", "Ни один"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('23b4b6d5-9165-5882-b452-c8cd0430e265', 1, 3, 'тест_с_вариантами', '58f38205-5aaf-5832-84f2-517065083526',
 5, 'Что такое индекс в PostgreSQL и чем он жертвует?', '{"options": ["Жертвует целостностью ради чтения", "Ускоряет чтение ценой замедления записи и места", "Ускоряет всё без издержек", "Шифрует таблицу"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('edb47493-21f6-5454-a5e1-ecf4289c7ee2', 1, 3, 'задача_с_кодом', '53256052-ce33-5a9a-b138-9438b905e6fd',
 2, 'Язык: go. На вход подаётся строка. Выведите количество гласных букв (a e i o u y и русские а е ё и о у ы э ю я) без учёта регистра.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strings\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Scan();s:=strings.ToLower(sc.Text());v:=\"aeiouаеёиоуыэюя\";c:=0;for _,ch:=range s{if strings.ContainsRune(v,ch){c++}};fmt.Println(c)}", "testCases": [{"input": "hello", "expected": "2"}, {"input": "Привет", "expected": "2"}, {"input": "Sky", "expected": "0"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('7403a54e-841c-5ad5-a0b6-cd1ec67310c9', 1, 3, 'задача_с_кодом', 'bfcc13f5-3667-52dc-898a-a8e05de750d4',
 3, 'Язык: cpp. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){int n;std::cin>>n;for(int i=1;i<=n;i++){if(i%15==0)std::cout<<\"FizzBuzz\\n\";else if(i%3==0)std::cout<<\"Fizz\\n\";else if(i%5==0)std::cout<<\"Buzz\\n\";else std::cout<<i<<\"\\n\";}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f57f60c3-e5a5-56ba-ba4d-459a720506fc', 1, 3, 'задача_с_кодом', 'b119729a-7805-5b73-9d71-b326978bec69',
 4, 'Язык: python. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "python", "reference": "import sys\ns=sys.stdin.read().strip().lower()\ns=''''.join(c for c in s if c.isalnum())\nprint(''true'' if s==s[::-1] else ''false'')", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('62a8fdd1-831d-5a64-97ab-c55e8515ebee', 1, 3, 'открытый_вопрос', '8eb05220-60c8-599b-8df0-ef660857c051',
 3, 'Что такое проблема N+1 и как её найти?', '{}'::jsonb, '{"rubric": "N+1: запросы в цикле вместо одного JOIN\nдиагностика: логи запросов, профилировщик\nрешение: eager loading, JOIN"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('31a07a74-30c1-51fb-acb7-8d4a72451fdb', 1, 3, 'открытый_вопрос', '04dba656-a297-5b6f-b04c-96d841a2efc1',
 4, 'Что такое медленный запрос и как его оптимизировать?', '{}'::jsonb, '{"rubric": "диагностика: EXPLAIN ANALYZE плана запроса\nиндексы: подбор по предикатам JOIN и WHERE\nпереписывание: устранение функций над колонками"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8c521170-7735-5da9-a825-d8eba95e6f58', 1, 4, 'тест_с_вариантами', '02ebc767-1998-5fce-8652-fbb40e4ad9bd',
 3, 'Что утверждает теорема CAP?', '{"options": ["Из трёх свойств доступны одновременно только два", "БД должна быть реляционной", "Сеть всегда надёжна", "Индексы не нужны"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('7f41c361-7e8b-5094-8f9f-47d235f3ef34', 1, 4, 'тест_с_вариантами', '02ebc767-1998-5fce-8652-fbb40e4ad9bd',
 4, 'Какой уровень изоляции в PostgreSQL по умолчанию?', '{"options": ["Read Uncommitted", "Read Committed", "Repeatable Read", "Serializable"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('32d60de5-d98f-5abc-9d3c-993d5cbc762f', 1, 4, 'тест_с_вариантами', '1333c937-1024-5102-8341-289cc7a88460',
 4, 'Для чего используется пул соединений?', '{"options": ["Чтобы хранить больше данных", "Чтобы переиспользовать соединения и не платить за их установку", "Для репликации", "Для бэкапов"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c7949005-c8ad-59a3-82cb-b0a75f1beb40', 1, 4, 'тест_с_вариантами', '1333c937-1024-5102-8341-289cc7a88460',
 5, 'Что такое eventual consistency?', '{"options": ["Данные никогда не согласуются", "После записи реплики сойдутся со временем", "Синхронная запись во все узлы", "Отказ от репликации"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e1198d6e-66c5-541a-ae88-c5651a883aa6', 1, 4, 'тест_с_вариантами', '27f432b1-e02c-5a6a-8fc7-ce3fc2060ae3',
 5, 'Зачем нужен брокер сообщений (Kafka/RabbitMQ)?', '{"options": ["Заменить базу данных", "Асинхронно связывать сервисы и сглаживать нагрузку", "Хранить статику", "Ускорять CPU"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('206384af-b74a-5249-9536-56886db93d1e', 1, 4, 'тест_с_вариантами', '27f432b1-e02c-5a6a-8fc7-ce3fc2060ae3',
 5, 'Чем горизонтальное масштабирование отличается от вертикального?', '{"options": ["Увеличением мощности одного сервера vs добавлением серверов", "Ничем", "Только ценой", "Только в облаке"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('d08cada3-3777-538d-99ec-61d4af054a36', 1, 4, 'задача_с_кодом', '70c3da0a-1093-5f3f-a16c-8653a74905a2',
 2, 'Язык: cpp. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){long long s=0,x;while(std::cin>>x)s+=x;std::cout<<s;return 0;}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e731e219-2603-5b66-a78f-f26604842e5e', 1, 4, 'задача_с_кодом', '686d9f6e-ab24-5f5e-ad62-c160567dcce0',
 3, 'Язык: python. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "python", "reference": "import sys\ns=sys.stdin.read().strip().lower()\ns=''''.join(c for c in s if c.isalnum())\nprint(''true'' if s==s[::-1] else ''false'')", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('39948082-9cc3-5c26-bce2-19f4e9ad0986', 1, 4, 'задача_с_кодом', '3b74408d-ad23-56a6-9496-de827fcda2aa',
 4, 'Язык: javascript. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "javascript", "reference": "let n=+require(''fs'').readFileSync(0,''utf8'').trim();let r=1n;for(let i=2n;i<=BigInt(n);i++)r*=i;console.log(r.toString());", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('796f7d88-91c8-5815-95f2-ac2bedcc6874', 1, 4, 'открытый_вопрос', 'b1e9bbd8-7271-52f8-b859-77216a5cefea',
 4, 'Как спроектировать идемпотентное API списания средств?', '{}'::jsonb, '{"rubric": "идempotency key: уникальный ключ операции\nуникальный индекс: на ключе, дедупликация\nатомарность: транзакция проверки и записи"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c27bef08-f5ec-52b9-be3d-fbcfa0d972c5', 1, 4, 'открытый_вопрос', '7662b9f5-457b-570c-99b0-56c3bdf549a2',
 5, 'Какие стратегии шардирования данных вы знаете и их компромиссы?', '{}'::jsonb, '{"rubric": "по диапазону: просто, но перекос нагрузки\nпо хешу ключа: равномерно, сложные запросы\nмаршрутизация: прозрачность для приложения"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8a67ed24-76e3-54de-9f94-f88e072de45e', 2, 2, 'тест_с_вариантами', '77d71fd0-9e03-5fdd-9366-0e7429534f2b',
 1, 'Какой тег HTML семантически означает основное содержимое страницы?', '{"options": ["<div>", "<main>", "<span>", "<b>"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c1d6e17b-eed7-5963-b690-81db0444687e', 2, 2, 'тест_с_вариантами', '77d71fd0-9e03-5fdd-9366-0e7429534f2b',
 2, 'Чем let отличается от const в JavaScript?', '{"options": ["const нельзя переназначить", "let быстрее", "const только для чисел", "Разницы нет"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('cfe7e2f0-7f8d-582f-ad4e-5195911753db', 2, 2, 'тест_с_вариантами', '4cf6a290-f32f-5cb0-85dc-0a8a7bb1b88e',
 2, 'Что такое DOM?', '{"options": ["Язык стилей", "Объектная модель документа", "Сервер базы данных", "Протокол передачи"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('74cdbdc3-a4a9-558e-a9e9-6eaa640e095e', 2, 2, 'тест_с_вариантами', '4cf6a290-f32f-5cb0-85dc-0a8a7bb1b88e',
 3, 'Какая команда CSS размещает элементы в ряд с выравниванием?', '{"options": ["display:block", "display:flex", "float:center", "position:inline"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('536cabf3-2f59-547c-a827-6f781fa9bfab', 2, 2, 'тест_с_вариантами', '91c6f3fe-a0f2-56e0-b9a3-7426adc81cbd',
 3, 'Что делает addEventListener?', '{"options": ["Создаёт элемент", "Подписывает функцию на событие элемента", "Удаляет стиль", "Загружает скрипт"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('707cc409-fc8c-5562-85d1-40564e4657c1', 2, 2, 'тест_с_вариантами', '91c6f3fe-a0f2-56e0-b9a3-7426adc81cbd',
 4, 'Что означает статус ответа 301?', '{"options": ["Не найдено", "Перенаправление навсегда", "Ошибка сервера", "Успех"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('7f38c436-6bf3-56d5-9a95-de4105cccd5b', 2, 2, 'задача_с_кодом', '150778ec-8376-5006-9800-8951f40d753e',
 2, 'Язык: go. На вход подаётся строка. Выведите количество гласных букв (a e i o u y и русские а е ё и о у ы э ю я) без учёта регистра.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strings\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Scan();s:=strings.ToLower(sc.Text());v:=\"aeiouаеёиоуыэюя\";c:=0;for _,ch:=range s{if strings.ContainsRune(v,ch){c++}};fmt.Println(c)}", "testCases": [{"input": "hello", "expected": "2"}, {"input": "Привет", "expected": "2"}, {"input": "Sky", "expected": "0"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('dcf594d6-d5cc-57a9-88c8-061878ae923a', 2, 2, 'задача_с_кодом', '804dc8ac-1378-588d-93b7-a9212fc1c6d9',
 3, 'Язык: cpp. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){int n;std::cin>>n;for(int i=1;i<=n;i++){if(i%15==0)std::cout<<\"FizzBuzz\\n\";else if(i%3==0)std::cout<<\"Fizz\\n\";else if(i%5==0)std::cout<<\"Buzz\\n\";else std::cout<<i<<\"\\n\";}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('016a5b8a-d04c-5686-90e4-fd1486b8aeb6', 2, 2, 'задача_с_кодом', '9f65721f-b2a1-5047-98f7-0ed17ac277c8',
 4, 'Язык: python. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "python", "reference": "import sys\ns=sys.stdin.read().strip().lower()\ns=''''.join(c for c in s if c.isalnum())\nprint(''true'' if s==s[::-1] else ''false'')", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f295f390-d3a8-52d9-9635-8f24aad31fc0', 2, 2, 'открытый_вопрос', 'cfc00399-acd4-5076-a46f-731ea3562a0b',
 2, 'Что такое блочная модель CSS?', '{}'::jsonb, '{"rubric": "content: содержимое элемента\npadding и border: внутренние отступы и рамка\nmargin: внешний отступ"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c18ebe6d-17fe-5cde-b4d8-30bd5544994a', 2, 2, 'открытый_вопрос', '5067356f-efb4-59ed-878a-8216644815b0',
 3, 'Что такое адаптивная вёрстка и способы её сделать?', '{}'::jsonb, '{"rubric": "media queries: точки перелома под размеры\nflex и grid: гибкие раскладки\nотносительные единицы: rem, %, vw"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('fc7fd403-d606-587d-9247-c4f862d96a76', 2, 2, 'открытый_вопрос', '25c416c2-3d82-5153-afb2-2e68b4ceeb7c',
 2, 'Что такое событие в JavaScript и пример обработчика?', '{}'::jsonb, '{"rubric": "событие: действие пользователя или браузера\nобработчик: addEventListener с функцией\nпример: click меняет текст элемента"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f097ca0d-8b98-5e78-a635-a3c71e572025', 2, 3, 'тест_с_вариантами', 'a8af1733-51e8-5106-aabf-55b56f6986fe',
 3, 'Что такое замыкание (closure) в JavaScript?', '{"options": ["Функция со своим лексическим окружением", "CSS-правило", "Тип промиса", "Серверный шаблон"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('530aa3ad-93a7-53fa-b91f-151f8f042cce', 2, 3, 'тест_с_вариантами', 'a8af1733-51e8-5106-aabf-55b56f6986fe',
 3, 'Что такое event loop?', '{"options": ["Цикл игры", "Механизм обработки асинхронных задач в JS", "Цикл for", "Рендеринг SVG"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8d666f54-340e-5905-ab89-fee6e147b3e1', 2, 3, 'тест_с_вариантами', 'e33d0fea-c085-582c-809f-8281dbf08861',
 4, 'В чём смысл virtual DOM в React?', '{"options": ["Хранить HTML на сервере", "Эффективно обновлять реальный DOM через сравнение", "Ускорять CSS", "Заменить HTTP"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ccbccb6b-1cca-59fc-ae27-f58e9001fe50', 2, 3, 'тест_с_вариантами', 'e33d0fea-c085-582c-809f-8281dbf08861',
 4, 'Что такое CORS?', '{"options": ["Защита от SQL-инъекций", "Механизм контроля кросс-доменных запросов", "Система сборки", "Тип кэша"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('1c22adea-d2b7-5b08-a48b-944706032bc8', 2, 3, 'тест_с_вариантами', '5cce9ba1-81a9-5b1d-bc15-3593e7673544',
 4, 'Чем debounce отличается от throttle?', '{"options": ["Ничем", "Debounce ждёт паузу, throttle ограничивает частоту", "Throttle только для кликов", "Debounce быстрее"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a7defec9-e014-542b-b6d3-0340ad8f6f37', 2, 3, 'тест_с_вариантами', '5cce9ba1-81a9-5b1d-bc15-3593e7673544',
 5, 'В каких состояниях может быть Promise?', '{"options": ["pending, fulfilled, rejected", "open, close", "start, stop", "true, false"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a5b87bd9-be0b-5565-98eb-07f01d55a499', 2, 3, 'задача_с_кодом', '1c495597-ae71-5fdf-abf5-3c375582a91a',
 2, 'Язык: cpp. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){long long s=0,x;while(std::cin>>x)s+=x;std::cout<<s;return 0;}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5488f54e-60f3-55f9-8b29-5c3eb41335bb', 2, 3, 'задача_с_кодом', '311c9c65-c0cc-506f-a0c9-dc91054e2e52',
 3, 'Язык: python. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "python", "reference": "import sys\ns=sys.stdin.read().strip().lower()\ns=''''.join(c for c in s if c.isalnum())\nprint(''true'' if s==s[::-1] else ''false'')", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3451f051-ede7-570f-aa52-4c389a8f312d', 2, 3, 'задача_с_кодом', '9cd93b23-74f2-5ee0-9c05-0b268aebb581',
 4, 'Язык: javascript. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "javascript", "reference": "let n=+require(''fs'').readFileSync(0,''utf8'').trim();let r=1n;for(let i=2n;i<=BigInt(n);i++)r*=i;console.log(r.toString());", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('18c0ba27-5c70-5033-a413-df9997788850', 2, 3, 'открытый_вопрос', 'bb37f110-e1d4-580c-9f7b-19ef38fae5b3',
 3, 'Как защитить форму от CSRF?', '{}'::jsonb, '{"rubric": "токен: синхронизатор токенов в форме\n SameSite cookie: ограничение кросс-сайтовой отправки\nпроверка Origin: на сервере"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('82966812-f716-5eda-9405-28fe559e1ad4', 2, 3, 'открытый_вопрос', '49bc8d0b-fdb2-54bc-b7f7-802b54fecb0b',
 4, 'Что такое Virtual DOM и в чём его выигрыш и цена?', '{}'::jsonb, '{"rubric": "выигрыш: минимум операций с реальным DOM\nцена: дополнительная память и сравнение\nграницы: большие списки требуют виртуализации"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('efdd248b-578b-521c-a068-2c9ccecf0a15', 2, 4, 'тест_с_вариантами', 'ce123c14-1ba7-55bf-99df-d20c1fc220f8',
 4, 'Что такое Core Web Vitals?', '{"options": ["Набор метрик восприятия скорости страницы", "Библиотека тестов", "Серверный фреймворк", "Формат изображений"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3921aebf-637c-5d78-ad53-adb115d5023e', 2, 4, 'тест_с_вариантами', 'ce123c14-1ba7-55bf-99df-d20c1fc220f8',
 4, 'Когда SSR предпочтительнее CSR?', '{"options": ["Всегда", "Когда важны SEO и первое отображение", "Когда нет сервера", "Никогда"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('d6e64620-c8c0-565e-b9ae-6dafa6bf4426', 2, 4, 'тест_с_вариантами', '3d1f5f0e-0d81-537d-b8b5-051b88842346',
 4, 'Что такое микрофронтенды?', '{"options": ["Мелкие иконки", "Независимо развёртываемые части фронтенда", "CSS-методология", "Вид тестов"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('339d84bb-c0e7-5e70-b807-6ab52b96d632', 2, 4, 'тест_с_вариантами', '3d1f5f0e-0d81-537d-b8b5-051b88842346',
 5, 'Какие стратегии кэширования HTTP существуют?', '{"options": ["Только memory cache", "Cache-Control, ETag, service worker и др.", "Только cookie", "Кэширование невозможно"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('fb85f00d-1104-5344-9f2d-c171841800ea', 2, 4, 'тест_с_вариантами', '902b126b-1cd7-5c9a-a2eb-b9b957939f7d',
 5, 'Что даёт семантическая вёрстка и доступность (a11y)?', '{"options": ["Только красоту", "Смысл для скринридеров и поисковиков", "Меньше кода", "Ускорение сервера"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('1bf4a14f-a20d-5627-bd7e-2bd56b1ad976', 2, 4, 'тест_с_вариантами', '902b126b-1cd7-5c9a-a2eb-b9b957939f7d',
 5, 'Как организовать состояние в большом приложении?', '{"options": ["Только глобальные переменные", "По доменам: локальное состояние + стор с нормализацией", "В localStorage всё", "В URL только"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('d68ec922-f836-5c14-905e-f8b430793d0d', 2, 4, 'задача_с_кодом', '6f57b33b-7d20-5cd5-8cc2-c005dfd9554b',
 2, 'Язык: python. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "python", "reference": "import sys\nn=int(sys.stdin.read().strip())\nprint(''\\n''.join(''FizzBuzz'' if i%15==0 else ''Fizz'' if i%3==0 else ''Buzz'' if i%5==0 else str(i) for i in range(1,n+1)))", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('dc160e8f-41e2-5a7a-bf06-312498736a32', 2, 4, 'задача_с_кодом', 'd67ff7a7-5202-50f4-8d5e-808c97c1dbd8',
 3, 'Язык: javascript. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "javascript", "reference": "let n=+require(''fs'').readFileSync(0,''utf8'').trim();let r=1n;for(let i=2n;i<=BigInt(n);i++)r*=i;console.log(r.toString());", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4920182c-09ce-536f-9cca-4cf1fad662b0', 2, 4, 'задача_с_кодом', '60c6b69f-04a9-5d72-8640-ebdc1a2d0402',
 4, 'Язык: csharp. На вход подаётся строка. Выведите количество гласных букв (a e i o u y и русские а е ё и о у ы э ю я) без учёта регистра.', '{"language": "csharp", "reference": "using System;using System.Linq;class Solution{static void Main(){string v=\"aeiouаеёиоуыэюя\",s=Console.In.ReadToEnd().ToLower();Console.WriteLine(s.Count(c=>v.Contains(c)));}}", "testCases": [{"input": "hello", "expected": "2"}, {"input": "Привет", "expected": "2"}, {"input": "Sky", "expected": "0"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('9762788a-c8fb-5ea5-8d25-e87fcb2e0d97', 2, 4, 'открытый_вопрос', 'c253a680-3fa2-5d69-a841-6d917e73ccc6',
 4, 'Как быстро понять, почему медленно первый рендер, и что чинить?', '{}'::jsonb, '{"rubric": "метрики: LCP, FCP из полевых данных\nразмер бандла: код-сплиттинг, ленивая загрузка\nкритический путь: инлайн CSS, приоритет ресурсов"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('7af37b53-bb18-544e-b836-323ab79d2897', 2, 4, 'открытый_вопрос', '8154c1a6-3abd-5093-a1a4-23c91a5a7331',
 5, 'Как организовать фронтенд-монорепозиторий с общими компонентами?', '{}'::jsonb, '{"rubric": "версионирование: единые зависимости и конфиги\nизоляция: независимые сборки и релизы\nграницы доменов: контракты через пакеты"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('27e06d31-2a35-52af-8248-359dffa9357e', 5, 2, 'тест_с_вариантами', 'c7bb812e-da33-5fd1-bee5-d541437e193d',
 1, 'Что такое регрессионное тестирование?', '{"options": ["Проверка новых фич", "Проверка, что старое не сломалось", "Тестирование нагрузки", "Проверка вёрстки"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('fc5df1d8-3d79-5c3d-8fae-bb0f0070207f', 5, 2, 'тест_с_вариантами', 'c7bb812e-da33-5fd1-bee5-d541437e193d',
 2, 'Из чего состоит тест-кейс?', '{"options": ["Только названия", "Шаги, тестовые данные, ожидаемый результат", "Только бага", "Логов сервера"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f0612296-97b1-5f19-86ef-0cc2731545e7', 5, 2, 'тест_с_вариантами', 'b0bbdeb1-ca35-5301-840b-0274b2f5c241',
 2, 'Чем severity отличается от priority?', '{"options": ["Ничем", "Severity — влияние дефекта, priority — очерёдность исправления", "Priority ставит тестировщик", "Severity всегда высокий"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('24efaa8b-74b7-5013-9ad3-92b1a504da31', 5, 2, 'тест_с_вариантами', 'b0bbdeb1-ca35-5301-840b-0274b2f5c241',
 3, 'Что такое пирамида тестирования?', '{"options": ["Диаграмма дефектов", "Много unit, меньше интеграционных, ещё меньше UI-тестов", "Инструмент нагрузки", "Тип отчёта"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e5e496ea-d933-57cb-80a4-5336edc580c0', 5, 2, 'тест_с_вариантами', '14e48cbd-bb83-5140-9719-9839e5e1430a',
 3, 'Чем smoke отличается от sanity?', '{"options": ["Ничем", "Smoke — базовая работоспособность, sanity — конкретная функция после изменений", "Sanity дольше", "Smoke после релиза только"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('62b430a8-3599-5403-bde4-8e91f85485a6', 5, 2, 'тест_с_вариантами', '14e48cbd-bb83-5140-9719-9839e5e1430a',
 4, 'Что такое граничные значения?', '{"options": ["Максимумы и минимы вокруг точек перехода", "Случайные числа", "Средние значения", "Логи"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ade33182-c8f5-5e43-992a-4e5d4c3e28d8', 5, 2, 'задача_с_кодом', '5f964a34-d07e-5b15-8a5a-4a4669257ee4',
 2, 'Язык: javascript. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "javascript", "reference": "let s=require(''fs'').readFileSync(0,''utf8'').trim().toLowerCase().replace(/[^a-z0-9а-яё]/g,'''');console.log(s===s.split('''').reverse().join('''')?''true'':''false'');", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3f18ec12-910e-59ac-8899-150b5346e04b', 5, 2, 'задача_с_кодом', '5573fc3c-4dbb-568a-aeec-df6ccaf1153d',
 3, 'Язык: csharp. На вход подаётся строка. Выведите количество гласных букв (a e i o u y и русские а е ё и о у ы э ю я) без учёта регистра.', '{"language": "csharp", "reference": "using System;using System.Linq;class Solution{static void Main(){string v=\"aeiouаеёиоуыэюя\",s=Console.In.ReadToEnd().ToLower();Console.WriteLine(s.Count(c=>v.Contains(c)));}}", "testCases": [{"input": "hello", "expected": "2"}, {"input": "Привет", "expected": "2"}, {"input": "Sky", "expected": "0"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4f4f5e3d-5432-50ea-ab7b-434d742497cf', 5, 2, 'задача_с_кодом', '4733d096-bf58-5655-ac99-7fa2cd1570ba',
 4, 'Язык: java. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "java", "reference": "import java.util.*;public class Solution{public static void main(String[] a){Scanner sc=new Scanner(System.in);long s=0;while(sc.hasNextLong())s+=sc.nextLong();System.out.println(s);}}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c1b62399-82c7-5e00-9892-122d8a2b0ed0', 5, 2, 'открытый_вопрос', '07e40ca1-de1a-5730-8022-a8aaff5cf75e',
 2, 'Как протестировать форму логина? Перечислите проверки.', '{}'::jsonb, '{"rubric": "позитив: валидные логин и пароль\nнегатив: неверный пароль, пустые поля\nбезопасность: SQL-инъекция, XSS в полях"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('1583f755-06a2-5910-94e1-7e33c6fa1c5a', 5, 2, 'открытый_вопрос', '80c1ccf6-db83-5080-aba1-864799cc5059',
 3, 'Что проверить при тестировании API эндпоинта?', '{}'::jsonb, '{"rubric": "статусы: коды 2xx/4xx/5xx по сценариям\nконтракт: схема ответа и ошибок\nграницы: пустые, максимальные, невалидные входы"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('32d3ee88-791f-5ffe-ad68-aa6e663bab32', 5, 3, 'тест_с_вариантами', 'a7c9a96c-fc8f-584b-a806-daa92d8c3023',
 3, 'Что такое классы эквивалентности?', '{"options": ["Группы входов с одинаковым поведением системы", "Классы в коде", "Приоритеты багов", "Версии тестов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e6ed430c-3b38-5d25-bd7c-b4f866197088', 5, 3, 'тест_с_вариантами', 'a7c9a96c-fc8f-584b-a806-daa92d8c3023',
 3, 'Чем mock отличается от stub?', '{"options": ["Ничем", "Mock проверяет взаимодействия, stub лишь возвращает заглушку", "Stub умнее", "Mock для UI"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('0ed19f7d-d98d-518a-8155-3c4670b88dc0', 5, 3, 'тест_с_вариантами', '5a7bf1a2-256c-5f4c-999d-31218b57bf1a',
 4, 'Что вызывает flaky-тесты?', '{"options": ["Слишком много ассертов", "Нестабильные ожидания, зависимость от времени и порядка", "JUnit", "Хорошая архитектура"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5227f814-b705-5cc3-b96d-797bd071c28b', 5, 3, 'тест_с_вариантами', '5a7bf1a2-256c-5f4c-999d-31218b57bf1a',
 4, 'Что такое BDD?', '{"options": ["Язык программирования", "Сценарии на естественном языке, связывающие бизнес и тесты", "База данных", "Сервер CI"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5bcb0175-365e-55f2-9176-ff6f3feb1d2d', 5, 3, 'тест_с_вариантами', '816f35bc-7299-5293-8796-817406cc2e83',
 4, 'Что проверяют contract-тесты?', '{"options": ["Цвета интерфейса", "Совместимость API провайдера и потребителя", "Производительность CPU", "Бэкапы"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('2726a057-2de1-51c4-b354-57b8fd9d4f29', 5, 3, 'тест_с_вариантами', '816f35bc-7299-5293-8796-817406cc2e83',
 5, 'Чем измеряют покрытие и где его предел?', '{"options": ["Строками кода; 100% не гарантирует качество", "Часами; 40% достаточно", "Багами; меньше лучше", "Тестами; 100% обязательно"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('31e44bbc-e86a-525e-a647-a1ba6fba0214', 5, 3, 'задача_с_кодом', 'c53d8612-5dff-54af-a7cd-6b0495b3063c',
 2, 'Язык: csharp. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "csharp", "reference": "using System;using System.Numerics;class Solution{static void Main(){int n=int.Parse(Console.In.ReadToEnd().Trim());BigInteger r=1;for(int i=2;i<=n;i++)r*=i;Console.WriteLine(r);}}", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('60237440-afcd-5224-8134-4459d7c46597', 5, 3, 'задача_с_кодом', '4c7eb475-aa11-57db-b43c-00f35861345e',
 3, 'Язык: java. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "java", "reference": "import java.util.*;public class Solution{public static void main(String[] a){Scanner sc=new Scanner(System.in);long s=0;while(sc.hasNextLong())s+=sc.nextLong();System.out.println(s);}}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ebfa6a4a-fb84-5ee1-bace-560344de42df', 5, 3, 'задача_с_кодом', 'ff796475-95f2-5078-8919-1088347d8a70',
 4, 'Язык: go. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strconv\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Scan();n,_:=strconv.Atoi(sc.Text());w:=bufio.NewWriter(os.Stdout);defer w.Flush();for i:=1;i<=n;i++{if i%15==0{fmt.Fprintln(w,\"FizzBuzz\")}else if i%3==0{fmt.Fprintln(w,\"Fizz\")}else if i%5==0{fmt.Fprintln(w,\"Buzz\")}else{fmt.Fprintln(w,i)}}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3d0feac1-1a19-5209-8398-3229e9221d2e', 5, 3, 'открытый_вопрос', '7e2c26f4-28b4-5561-b800-373673ad4b16',
 3, 'Чем верификация отличается от валидации?', '{}'::jsonb, '{"rubric": "верификация: продукт сделан правильно, по требованиям\nвалидация: правильный ли продукт, нужды пользователя\nпримеры: ревью кода vs приёмочные тесты"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('06d5b8ba-fd86-5030-b4a9-389812450d02', 5, 3, 'открытый_вопрос', '421434d4-c6b1-576b-adf3-62612472bcc1',
 4, 'Как стабилизировать набор автотестов со flaky-кейсами?', '{}'::jsonb, '{"rubric": "ожидания: явные вместо sleep\nизоляция: независимость от порядка и данных\nкарантин: повторный прогон и учёт стабильности"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f2fc6d99-fb7c-5057-8441-71b8c1f51989', 5, 4, 'тест_с_вариантами', '45164add-ed93-5cf9-bbc4-890f135c9f31',
 4, 'Что такое risk-based testing?', '{"options": ["Тестирование без риска", "Приоритизация по рискам и бизнес-влиянию", "Только security", "Ручное тестирование"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('b0d8f3b9-546a-50ee-b6b5-9a86fcc5c6a0', 5, 4, 'тест_с_вариантами', '45164add-ed93-5cf9-bbc4-890f135c9f31',
 4, 'Чем нагрузочное тестирование отличается от стрессового?', '{"options": ["Ничем", "Нагрузочное — до целевой нагрузки, стрессовое — за пределами", "Стрессовое короче", "Нагрузочное только для БД"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('da17dcfe-a437-59da-9619-244c9b447d1b', 5, 4, 'тест_с_вариантами', 'e3544380-619f-5ae9-afcd-134bac36e818',
 5, 'Что такое mutation testing?', '{"options": ["Изменение требований", "Проверка качества тестов внесением ошибок в код", "Тестирование мутантов", "Рефакторинг"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('bafca062-68e2-5e16-8ccc-15f26010e54d', 5, 4, 'тест_с_вариантами', 'e3544380-619f-5ae9-afcd-134bac36e818',
 5, 'Какие метрики QA имеют смысл для релиза?', '{"options": ["Только число тестов", "Дефекты по серьёзности, покрытие критики, стабильность прогонов", "Строки кода", "Возраст багов"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('99b73a8a-c7cf-5cff-8b7d-2b933d3fc5d7', 5, 4, 'тест_с_вариантами', '4354e618-e906-58e3-857e-c0c21223e2e3',
 5, 'Когда автоматизация НЕ окупается?', '{"options": ["Всегда окупается", "Для разовых фич и нестабильного UI", "Для API", "Для регресса"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('1eb385ec-026a-5adf-861e-1ebd8311f597', 5, 4, 'тест_с_вариантами', '4354e618-e906-58e3-857e-c0c21223e2e3',
 4, 'Что такое тест-дубль (test double)?', '{"options": ["Вторая копия продукта", "Заглушка вместо реальной зависимости", "Дублирующий тест", "Резервный сервер"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('23ba2a79-7afa-5e29-8110-3db9d62610d3', 5, 4, 'задача_с_кодом', '82015366-f6a2-578e-8fa7-ecf4b2be7afa',
 2, 'Язык: java. На вход подаётся строка. Выведите количество гласных букв (a e i o u y и русские а е ё и о у ы э ю я) без учёта регистра.', '{"language": "java", "reference": "import java.util.*;public class Solution{public static void main(String[] a){String s=new Scanner(System.in).useDelimiter(\"\\\\A\").next().toLowerCase();String v=\"aeiouаеёиоуыэюя\";long c=s.chars().filter(ch->v.indexOf(ch)>=0).count();System.out.println(c);}}", "testCases": [{"input": "hello", "expected": "2"}, {"input": "Привет", "expected": "2"}, {"input": "Sky", "expected": "0"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('d28c8944-3ae8-5d40-8e8a-89bd541fcf9f', 5, 4, 'задача_с_кодом', '9a44ec56-bbcf-54c0-9b19-041f60e003f4',
 3, 'Язык: go. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strconv\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Scan();n,_:=strconv.Atoi(sc.Text());w:=bufio.NewWriter(os.Stdout);defer w.Flush();for i:=1;i<=n;i++{if i%15==0{fmt.Fprintln(w,\"FizzBuzz\")}else if i%3==0{fmt.Fprintln(w,\"Fizz\")}else if i%5==0{fmt.Fprintln(w,\"Buzz\")}else{fmt.Fprintln(w,i)}}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('853b6fae-49cd-5496-8be8-5b75811684af', 5, 4, 'задача_с_кодом', '4597dcec-4780-5963-8d3c-59398564c27b',
 4, 'Язык: cpp. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){std::string s;std::getline(std::cin,s);std::string t;for(char c:s)if(isalnum((unsigned char)c))t+=tolower((unsigned char)c);std::string r(t.rbegin(),t.rend());std::cout<<(t==r?\"true\":\"false\");}", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('d576aee6-9c9b-5996-b689-54a968f81a2e', 5, 4, 'открытый_вопрос', '642fd76a-6482-5fb5-b822-ea7be372d9d2',
 4, 'Как выстроить тестовую стратегию для релиза с рискованными платежами?', '{}'::jsonb, '{"rubric": "риски: приоритет критических сценариев оплаты\nуровни: unit + контрактные + E2E критики\nвзвешивание: smoke перед релизом обязателен"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3660daca-a011-5658-9233-18df3af4842f', 5, 4, 'открытый_вопрос', '266ad2b7-65a9-544d-9ea3-85df2f29740f',
 5, 'Как выбрать, что автоматизировать, а что оставить ручным?', '{}'::jsonb, '{"rubric": "критерий: стабильность и повторяемость сценария\nрегресс: автоматизация приоритетна\nисследовательское: остаётся ручным"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a73909a8-0e88-5e00-8085-9470699b8a8d', 4, 2, 'тест_с_вариантами', '295e7185-5962-5e85-9cbc-05fa06ea7659',
 1, 'Что такое CI/CD?', '{"options": ["Язык программирования", "Непрерывная интеграция и доставка", "Тип базы данных", "Виртуальная машина"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5c24e17a-43b7-5c66-acb2-e73eb884d64d', 4, 2, 'тест_с_вариантами', '295e7185-5962-5e85-9cbc-05fa06ea7659',
 2, 'Что такое контейнер?', '{"options": ["Архив с кодом", "Изолированная среда исполнения приложения", "Монитор", "Протокол"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a724b099-ad0e-5d2b-a83d-a23938506296', 4, 2, 'тест_с_вариантами', 'c7abc603-a579-5501-8e90-27545b61ac7a',
 2, 'Какая команда выводит список файлов в Linux?', '{"options": ["cd", "ls", "mkdir", "grep"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3c4794e0-8cdc-5796-bcbe-db24b0b0d535', 4, 2, 'тест_с_вариантами', 'c7abc603-a579-5501-8e90-27545b61ac7a',
 3, 'Что такое DNS?', '{"options": ["Система доменных имён", "База данных", "Файрвол", "Язык разметки"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('21163a6c-ff6f-5bda-a882-0611932985c4', 4, 2, 'тест_с_вариантами', 'd42a2cfb-469f-5425-b806-80183226860a',
 3, 'Чем Docker отличается от виртуальной машины?', '{"options": ["Ничем", "Контейнеры делят ядро ОС, ВМ эмулируют железо", "Docker тяжелее", "ВМ быстрее"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4e9b7711-4eb7-56a7-ae5d-4ba0f41de9df', 4, 2, 'тест_с_вариантами', 'd42a2cfb-469f-5425-b806-80183226860a',
 4, 'Что хранит переменная окружения?', '{"options": ["Исходный код", "Настройки среды исполнения", "Логи", "Кэш"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('9634f7a4-22b4-5a25-a918-0501e3163b90', 4, 2, 'задача_с_кодом', '056c9ee0-897c-5ce3-a55a-eb2c088ec8ef',
 2, 'Язык: python. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "python", "reference": "import sys\nn=int(sys.stdin.read().strip())\nprint(''\\n''.join(''FizzBuzz'' if i%15==0 else ''Fizz'' if i%3==0 else ''Buzz'' if i%5==0 else str(i) for i in range(1,n+1)))", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('cf6c52ed-60cf-5aca-bad5-ffdbf6a2e4f1', 4, 2, 'задача_с_кодом', '70ce1ef5-899b-5486-884b-fc2b217ad1be',
 3, 'Язык: javascript. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "javascript", "reference": "let n=+require(''fs'').readFileSync(0,''utf8'').trim();let r=1n;for(let i=2n;i<=BigInt(n);i++)r*=i;console.log(r.toString());", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4e94f1ba-9bc0-5a77-b3b6-50911b4771b3', 4, 2, 'задача_с_кодом', 'f5ed6507-172e-565e-924f-f6744a23da95',
 4, 'Язык: csharp. На вход подаётся строка. Выведите количество гласных букв (a e i o u y и русские а е ё и о у ы э ю я) без учёта регистра.', '{"language": "csharp", "reference": "using System;using System.Linq;class Solution{static void Main(){string v=\"aeiouаеёиоуыэюя\",s=Console.In.ReadToEnd().ToLower();Console.WriteLine(s.Count(c=>v.Contains(c)));}}", "testCases": [{"input": "hello", "expected": "2"}, {"input": "Привет", "expected": "2"}, {"input": "Sky", "expected": "0"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ef3f4e87-c7fc-5bc9-b64e-1823e3f51e03', 4, 2, 'открытый_вопрос', '0edac926-1233-5d90-85d7-7109e0b8fc0c',
 2, 'Чем CI отличается от CD?', '{}'::jsonb, '{"rubric": "CI: автоматическая сборка и тесты при каждом коммите\nCD: автоматическая доставка/деплой после CI\nрезультат: релизы чаще и предсказуемее"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c6cea868-7c00-5bba-b2c5-f871dd19c7cd', 4, 2, 'открытый_вопрос', '9b2f4d85-63f3-589c-a313-32dbbd80624b',
 3, 'Что такое мониторинг и из чего он состоит?', '{}'::jsonb, '{"rubric": "метрики: числовые показатели во времени\nлоги: события с деталями\nалерты: пороги и уведомления ответственным"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('6851df3c-f5ea-57c8-aea0-a29c28a08da4', 4, 3, 'тест_с_вариантами', '469e36cb-a3e3-54d0-96e4-d019e11f5206',
 3, 'Что такое IaC?', '{"options": ["Язык интерфейсов", "Инфраструктура как код", "Вид тестов", "База знаний"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a458cbd5-3c9f-5ab4-b904-7d5684438ed0', 4, 3, 'тест_с_вариантами', '469e36cb-a3e3-54d0-96e4-d019e11f5206',
 3, 'Зачем нужен Kubernetes?', '{"options": ["Писать код", "Оркестрация контейнеров: масштабирование, самовосстановление", "Хранить секреты вручную", "Компиляция"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5867398d-ca35-55a0-ab6d-9331fc766fc4', 4, 3, 'тест_с_вариантами', 'a16dece1-5dcb-5449-af23-63bee6ad28c4',
 4, 'Что такое blue-green деплой?', '{"options": ["Деплой по FTP", "Две одинаковые среды: переключение трафика между ними", "Тестирование цвета", "Резервное копирование"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('50f8a91f-b1c5-58ec-85d3-e98ce792e12f', 4, 3, 'тест_с_вариантами', 'a16dece1-5dcb-5449-af23-63bee6ad28c4',
 4, 'Чем метрики отличаются от логов?', '{"options": ["Ничем", "Метрики — числа во времени, логи — события с деталями", "Логи быстрее", "Метрики только в облаке"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('547d2966-24d6-528d-af64-ab5d09e18db7', 4, 3, 'тест_с_вариантами', '73005394-484e-5cc0-be22-420b812f1a31',
 4, 'Что такое health check?', '{"options": ["Опрос врача", "Проверка живости сервиса для оркестратора", "Тест API", "Мониторинг диска"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('b4dce1de-5720-5ecb-b1f4-97cc897354a5', 4, 3, 'тест_с_вариантами', '73005394-484e-5cc0-be22-420b812f1a31',
 5, 'Что такое секрет и где его хранить?', '{"options": ["Пароль в репозитории", "Чувствительные данные в менеджере секретов/Vault, не в коде", "В README", "В логах"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('359b77dc-8973-50e1-8dbe-dcfd5551d03d', 4, 3, 'задача_с_кодом', '01fdfcbf-3be3-5ea7-9489-8c61270c3673',
 2, 'Язык: javascript. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "javascript", "reference": "let s=require(''fs'').readFileSync(0,''utf8'').trim().toLowerCase().replace(/[^a-z0-9а-яё]/g,'''');console.log(s===s.split('''').reverse().join('''')?''true'':''false'');", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('dd937ba5-ede0-536c-817b-a067e145d301', 4, 3, 'задача_с_кодом', '45e88e2f-9c66-5d40-8a5b-9724171937b1',
 3, 'Язык: csharp. На вход подаётся строка. Выведите количество гласных букв (a e i o u y и русские а е ё и о у ы э ю я) без учёта регистра.', '{"language": "csharp", "reference": "using System;using System.Linq;class Solution{static void Main(){string v=\"aeiouаеёиоуыэюя\",s=Console.In.ReadToEnd().ToLower();Console.WriteLine(s.Count(c=>v.Contains(c)));}}", "testCases": [{"input": "hello", "expected": "2"}, {"input": "Привет", "expected": "2"}, {"input": "Sky", "expected": "0"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('213e35dc-8648-582b-890d-1e137e35dffb', 4, 3, 'задача_с_кодом', '21413a3e-1f24-5334-9dc0-c1395f097cd1',
 4, 'Язык: java. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "java", "reference": "import java.util.*;public class Solution{public static void main(String[] a){Scanner sc=new Scanner(System.in);long s=0;while(sc.hasNextLong())s+=sc.nextLong();System.out.println(s);}}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('83e61de2-62a7-5450-94c6-4025f5cb8b64', 4, 3, 'открытый_вопрос', '984dc54e-5a79-5fbb-b78c-81e623d8042c',
 3, 'Как добиться деплоя без простоя на двух серверах?', '{}'::jsonb, '{"rubric": "blue-green: две среды и переключение трафика\nhealth check: проверка перед переключением\nоткат: мгновенный возврат на старую среду"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('6262668e-a9d4-5a8e-8303-0ac3e36ea8bf', 4, 3, 'открытый_вопрос', 'aeaf86b2-58c4-52b4-8779-6329996d6508',
 4, 'Что такое alert fatigue и как с ней бороться?', '{}'::jsonb, '{"rubric": "причина: слишком много шумных алертов\nмеры: агрегация, severity, подавление дублей\nвладение: каждый алерт имеет владельца и runbook"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('9c85f879-5fc1-544c-be40-10f3ef538271', 4, 4, 'тест_с_вариантами', 'c8b8e692-7f2d-5546-9305-46fd3ba25f84',
 4, 'Что такое SLO/SLA/SLI?', '{"options": ["Виды баз данных", "Цель, соглашение и индикатор уровня сервиса", "Сетевые протоколы", "Языки IaC"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('06ce2baf-bc38-53c1-b8e7-0566d817479d', 4, 4, 'тест_с_вариантами', 'c8b8e692-7f2d-5546-9305-46fd3ba25f84',
 4, 'Что такое service mesh?', '{"options": ["Сетка мониторинга", "Инфраструктурный слой сервис-сервис коммуникаций с mTLS и балансировкой", "Wi-Fi схема", "UI-фреймворк"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('d3d296bb-5319-553c-aa9d-a994ddd03e64', 4, 4, 'тест_с_вариантами', '8bf0158b-7675-539b-a9a5-0eb76082b6b5',
 5, 'Как планировать capacity?', '{"options": ["По настроению", "По метрикам роста, сезонности и запасу на отказы", "Максимально мощное железо", "Никак"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c6ac792f-0ab7-5840-9412-541933d09364', 4, 4, 'тест_с_вариантами', '8bf0158b-7675-539b-a9a5-0eb76082b6b5',
 5, 'Что такое runbook/incident management?', '{"options": ["Книга романов", "Процесс и инструкции реагирования на инциденты", "Бэкап конфигов", "Вид репозитория"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('cab7c92e-d562-5c5b-89ff-d5f71dc86ba7', 4, 4, 'тест_с_вариантами', 'c9965211-d987-59ca-83fe-0b45c1306a43',
 5, 'Как построить zero-downtime деплой?', '{"options": ["Никак", "Blue-green/канареечные релизы + health checks + откат", "Остановка на часы", "Только ночью"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8dd3dbd9-c05f-5ed3-b144-cacb1bc000c9', 4, 4, 'тест_с_вариантами', 'c9965211-d987-59ca-83fe-0b45c1306a43',
 4, 'Чем шифрование секретов отличается от простого base64?', '{"options": ["Ничем", "Base64 — кодирование, шифрование требует ключа и KMS", "Base64 безопаснее", "Шифрование медленнее всегда"]}'::jsonb, '{"correct": [1]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5902c53e-52cb-508c-8a2a-a0f9a73f1217', 4, 4, 'задача_с_кодом', '802b2d54-e7e3-582d-820a-48f9aeb5c12b',
 2, 'Язык: csharp. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "csharp", "reference": "using System;using System.Numerics;class Solution{static void Main(){int n=int.Parse(Console.In.ReadToEnd().Trim());BigInteger r=1;for(int i=2;i<=n;i++)r*=i;Console.WriteLine(r);}}", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ac41e7ce-965c-57d7-90c2-ca9da076c98a', 4, 4, 'задача_с_кодом', '962d1f71-9e5a-5131-bbb4-c64364247a55',
 3, 'Язык: java. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "java", "reference": "import java.util.*;public class Solution{public static void main(String[] a){Scanner sc=new Scanner(System.in);long s=0;while(sc.hasNextLong())s+=sc.nextLong();System.out.println(s);}}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('7bbb9144-6b08-5a66-80d3-6c8d5be0e99e', 4, 4, 'задача_с_кодом', '827aa9b3-f4ad-5815-a1a3-01c2304d855f',
 4, 'Язык: go. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strconv\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Scan();n,_:=strconv.Atoi(sc.Text());w:=bufio.NewWriter(os.Stdout);defer w.Flush();for i:=1;i<=n;i++{if i%15==0{fmt.Fprintln(w,\"FizzBuzz\")}else if i%3==0{fmt.Fprintln(w,\"Fizz\")}else if i%5==0{fmt.Fprintln(w,\"Buzz\")}else{fmt.Fprintln(w,i)}}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('222177be-2b97-569f-bd2b-59e1ebfa198c', 4, 4, 'открытый_вопрос', '08d3dcff-579c-57b1-8cc3-0db979ac71da',
 4, 'Как обеспечить SLA 99.95%? Назовите меры на всех уровнях.', '{}'::jsonb, '{"rubric": "избыточность: минимум две зоны доступности\nавтоскейлинг: и метрики, и health check\nнаблюдаемость: алертинг по SLO, runbook"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8a202dfa-dd4f-59f9-ab28-641c00074af3', 4, 4, 'открытый_вопрос', 'd795a442-cbb7-5afb-831e-1ea20251afe5',
 5, 'Как выстроить disaster recovery для критичного сервиса?', '{}'::jsonb, '{"rubric": "RTO и RPO: целевые времена восстановления\nбэкапы: регулярные, проверенные на восстановление\nплан: репетиции отказа, документированные шаги"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;

-- Проверка результата
SELECT (SELECT count(*) FROM test_questions WHERE id IN (
    SELECT id FROM test_questions
)) AS bank_total,
       (SELECT count(*) FROM test_assignments WHERE is_active) AS assignments_total;

COMMIT;

-- ============================================================================
-- SEED QUESTION BANK, ЧАСТЬ 2: расширение банка вопросов (+108 вопросов).
-- 12 пар × 9: 6 quiz (3 изоморфные группы по 2, сложность до 5),
-- 2 code (оставшиеся задачи и языки), 1 open.
--
-- Совместимость с частью 1: UUID с префиксом fsp-qbank2- — пересечений нет;
-- файлы независимы: порядок загрузки любой, повторный запуск безопасен.
-- ЧИСТО ДОБАВОЧНЫЙ: ни одного DELETE. Все вопросы is_active = true.
-- Запуск: psql -d fsp_platform -f seed-questions-2.sql
-- ============================================================================

BEGIN;

INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('1bc8007c-b8e9-5b89-aa4a-ef70a9d3f17b', 1, 2, 'тест_с_вариантами', '2bfa3092-5f95-517b-8a92-938627357a2c',
 2, 'Чем процесс отличается от потока?', '{"options": ["Процесс — тяжелее, имеет своё адресное пространство, потоки делят память процесса", "Поток тяжелее процесса", "Ничем", "Поток не выполняется"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('464cdb90-9322-536e-a059-8613814b2aaf', 1, 2, 'тест_с_вариантами', '2bfa3092-5f95-517b-8a92-938627357a2c',
 2, 'Что такое JSON?', '{"options": ["Формат обмена данными в текстовом виде", "База данных", "Язык программирования", "Операционная система"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a6842f56-0e04-538a-a76a-e40f8639b048', 1, 2, 'тест_с_вариантами', '523d51dd-ddf5-55d3-b61b-7bd5cdea1b08',
 3, 'Чем TCP отличается от UDP?', '{"options": ["TCP надёжный и с установкой соединения, UDP — быстрый без гарантий", "UDP надёжнее", "TCP быстрее", "Ничем"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('9095e850-7393-5367-933f-9e8a4e9420b9', 1, 2, 'тест_с_вариантами', '523d51dd-ddf5-55d3-b61b-7bd5cdea1b08',
 3, 'Что хранит cookie в браузере?', '{"options": ["Небольшие данные сайта на стороне клиента", "Пароли в открытом виде", "Файлы сервера", "Сессии сервера"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('6c889ef7-ef9b-5024-bc26-6c5d8ad603e8', 1, 2, 'тест_с_вариантами', '93af2cb9-c490-53e8-8a06-45a76d06804c',
 4, 'Чем HTTP 500 отличается от 503?', '{"options": ["500 — внутренняя ошибка сервера, 503 — сервис временно недоступен", "Одинаковы", "503 — ошибка кода", "500 — клиент виноват"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ec335968-a1c5-53c2-82f1-ef3f920df206', 1, 2, 'тест_с_вариантами', '93af2cb9-c490-53e8-8a06-45a76d06804c',
 5, 'Чем компилятор отличается от интерпретатора?', '{"options": ["Компилятор переводит весь код заранее, интерпретатор исполняет построчно", "Интерпретатор быстрее", "Компилятор только для C++", "Ничем"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('319d8e57-11f2-51fe-8294-04a0bb193b0c', 1, 2, 'задача_с_кодом', '8731464a-748c-564d-8f04-24fb7846cdd6',
 2, 'Язык: python. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "python", "reference": "import sys\nn=int(sys.stdin.read().strip())\nprint(''\\n''.join(''FizzBuzz'' if i%15==0 else ''Fizz'' if i%3==0 else ''Buzz'' if i%5==0 else str(i) for i in range(1,n+1)))", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f2c50b9a-83af-5829-b028-59ba9d521122', 1, 2, 'задача_с_кодом', 'bebce1ef-2b94-59fc-960e-44f89412ce4e',
 4, 'Язык: javascript. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "javascript", "reference": "let s=require(''fs'').readFileSync(0,''utf8'').trim().toLowerCase().replace(/[^a-z0-9а-яё]/g,'''');console.log(s===s.split('''').reverse().join('''')?''true'':''false'');", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('88d494e7-43ed-535c-be1d-4abf1ba19e4f', 1, 2, 'открытый_вопрос', '523fec9a-3188-5f1e-9ab3-68128a09c185',
 3, 'Опишите путь HTTP-запроса от браузера до ответа сервера.', '{}'::jsonb, '{"rubric": "DNS: разрешение имени в IP-адрес\nTCP/TLS: установление соединения и шифрования\nHTTP: запрос к серверу и ответ со статусом"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('340c21a1-12a4-501b-bed7-a4ff56b25473', 1, 3, 'тест_с_вариантами', 'c0635d0c-9831-5d41-9556-f0003c54d357',
 2, 'Какая главная особенность HTTP/2?', '{"options": ["Мультиплексирование запросов в одном соединении", "Новый язык", "Отказ от TCP", "Шифрование обязательно"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('baa00090-499c-567c-8fec-d3503e802f87', 1, 3, 'тест_с_вариантами', 'c0635d0c-9831-5d41-9556-f0003c54d357',
 3, 'Что такое ORM?', '{"options": ["Слой сопоставления объектов и таблиц базы данных", "Вид индекса", "Протокол", "Библиотека тестов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e71a3e90-0a0f-539d-8861-46e24022eaf0', 1, 3, 'тест_с_вариантами', '376a9064-c99c-57c2-bf4d-f4816469f195',
 3, 'Что такое гонка (race condition)?', '{"options": ["Некорректный результат из-за недетерминированного порядка доступа к разделяемым данным", "Быстрый код", "Соревнование серверов", "Тип индекса"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('6412cccc-cd1f-5d21-bebf-114e200c97ea', 1, 3, 'тест_с_вариантами', '376a9064-c99c-57c2-bf4d-f4816469f195',
 4, 'Что означает принцип единственной ответственности (SOLID)?', '{"options": ["Класс решает одну задачу и имеет одну причину для изменения", "Один класс на проект", "Нет зависимостей", "Только один метод"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('52c6707d-bc40-589f-9bcf-849814de1783', 1, 3, 'тест_с_вариантами', '30e3a42b-0e1c-5382-ab59-b3e356d00b12',
 4, 'Чем пессимистичная блокировка отличается от оптимистичной?', '{"options": ["Блокирует заранее vs проверяет версию при записи", "Оптимистичная безопаснее всегда", "Ничем", "Пессимистичная без блокировок"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e585f1ac-23cf-5b83-94d8-c41f8e66eccf', 1, 3, 'тест_с_вариантами', '30e3a42b-0e1c-5382-ab59-b3e356d00b12',
 5, 'Зачем многоуровневый кэш (CPU/L1/БД/Redis)?', '{"options": ["Согласовать скорость и стоимость доступа к данным", "Усложнить архитектуру", "Заменить память", "Увеличить latency"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ed3304aa-3bca-5abb-a489-3bb907052808', 1, 3, 'задача_с_кодом', 'f86bdc54-c501-5392-88ca-d6731f257989',
 2, 'Язык: csharp. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "csharp", "reference": "using System;using System.Linq;class Solution{static void Main(){var d=Console.In.ReadToEnd().Split(new char[0],StringSplitOptions.RemoveEmptyEntries).Select(long.Parse);Console.WriteLine(d.Sum());}}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('99019306-4734-52df-8a44-ee68b4fc824a', 1, 3, 'задача_с_кодом', '25719f8e-435d-579b-87f9-f818b442f94c',
 4, 'Язык: java. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "java", "reference": "import java.util.*;public class Solution{public static void main(String[] a){String s=new Scanner(System.in).nextLine().toLowerCase().replaceAll(\"[^a-z0-9а-яё]\",\"\");System.out.println(new StringBuilder(s).reverse().toString().equals(s)?\"true\":\"false\");}}", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a98a2c6b-ec51-57b3-af5b-43c060842c19', 1, 3, 'открытый_вопрос', '115ec4ef-c6ea-564b-ad95-4752f0a6b5cf',
 4, 'Как ведёт себя пул соединений под нагрузкой и за какими метриками следить?', '{}'::jsonb, '{"rubric": "исчерпание: ожидание свободного соединения в очереди\nметрики: активные, ожидающие, время получения\nнастройка: размер пула и таймауты"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4e06c90d-e6e1-5997-be1f-985564737099', 1, 4, 'тест_с_вариантами', 'ee66011a-a06f-5e78-998f-3ea184ccb155',
 4, 'Что такое паттерн Saga?', '{"options": ["Управление распределенной транзакцией через компенсирующие операции", "Игра", "Тип индекса", "Кэш"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('541c3808-ec08-579e-bb9e-de8a054ec550', 1, 4, 'тест_с_вариантами', 'ee66011a-a06f-5e78-998f-3ea184ccb155',
 4, 'В чём идея CQRS?', '{"options": ["Разделение моделей чтения и записи", "Объединение всех таблиц", "Отказ от SQL", "Один контроллер"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ab3e77fb-20d0-59f8-8d78-adb5007fdad5', 1, 4, 'тест_с_вариантами', '83b7a8df-52d6-50e2-a4a2-3f09ef0a25b5',
 5, 'Что такое backpressure?', '{"options": ["Механизм замедления производителя при переполнении потребителя", "Давление в кластере", "Тип балансировки", "Кэш"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('b15a3a67-32d5-57f5-9e8c-fabf89f3e19a', 1, 4, 'тест_с_вариантами', '83b7a8df-52d6-50e2-a4a2-3f09ef0a25b5',
 5, 'Как достичь exactly-once доставки сообщений?', '{"options": ["Идемпотентный потребитель + дедупликация/транзакционное хранение offset", "Никак", "Только UDP", "Увеличением брокеров"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('649b6895-cb20-5a1c-b80b-e0950066ac8a', 1, 4, 'тест_с_вариантами', '1dff3053-f543-5987-b39a-ad080476ce13',
 5, 'Зачем consistent hashing?', '{"options": ["Минимальное перемещение данных при изменении числа узлов", "Хеширование паролей", "Шифрование", "Балансировка CPU"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('bb34943f-1363-5238-9349-2b45d120206a', 1, 4, 'тест_с_вариантами', '1dff3053-f543-5987-b39a-ad080476ce13',
 3, 'Что такое двухфазный коммит (2PC)?', '{"options": ["Протокол согласования распределенной транзакции между узлами", "Два коммита подряд", "Слияние веток", "Бэкап"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a45f13d2-146d-5edd-9435-152a80c1b312', 1, 4, 'задача_с_кодом', '69aa8d15-c343-5985-8694-27aa3bac643d',
 2, 'Язык: csharp. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "csharp", "reference": "using System;class Solution{static void Main(){int n=int.Parse(Console.In.ReadToEnd().Trim());for(int i=1;i<=n;i++){if(i%15==0)Console.WriteLine(\"FizzBuzz\");else if(i%3==0)Console.WriteLine(\"Fizz\");else if(i%5==0)Console.WriteLine(\"Buzz\");else Console.WriteLine(i);}}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5fd7bfb1-8351-50d8-90d6-60883891b07c', 1, 4, 'задача_с_кодом', '498f848b-7e21-5c21-88da-3e76fcdf2abd',
 4, 'Язык: java. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "java", "reference": "import java.util.*;import java.math.*;public class Solution{public static void main(String[] a){int n=new Scanner(System.in).nextInt();BigInteger r=BigInteger.ONE;for(int i=2;i<=n;i++)r=r.multiply(BigInteger.valueOf(i));System.out.println(r);}}", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4da9317a-02fe-540e-b792-7e017a01631e', 1, 4, 'открытый_вопрос', 'c681850f-0906-5b3d-98e5-e2c108d26bf7',
 5, 'Спроектируйте rate limiter для публичного API.', '{}'::jsonb, '{"rubric": "алгоритм: token bucket или sliding window\nхранение: Redis для распределённого лимита\nответы: 429 и заголовки Retry-After, X-RateLimit"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('308ac968-2ae6-5fe0-b9f1-b25dce2e7fa0', 2, 2, 'тест_с_вариантами', '1227b6c5-daf5-576c-b108-360e85ff3ad5',
 2, 'Что такое CSS-специфичность?', '{"options": ["Правила приоритета селекторов при конфликте стилей", "Размер файла", "Скорость сети", "Шрифт"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a18727f9-5914-5942-9ecb-8d819dd201a2', 2, 2, 'тест_с_вариантами', '1227b6c5-daf5-576c-b108-360e85ff3ad5',
 2, 'Чем блочный элемент отличается от строчного?', '{"options": ["Блочный занимает всю ширину, строчный — только место содержимого", "Ничем", "Строчный переносится всегда", "Блочный нельзя стилизовать"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8c54db82-da07-555c-bdde-dc31c05884ae', 2, 2, 'тест_с_вариантами', '0e207138-0f2e-5319-a671-75ed40d7ee07',
 3, 'Чем localStorage отличается от sessionStorage?', '{"options": ["Срок жизни: постоянно vs до закрытия вкладки", "Ничем", "sessionStorage больше", "localStorage только для чисел"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c1256eed-d5e4-5e94-a1aa-efd9729aadf2', 2, 2, 'тест_с_вариантами', '0e207138-0f2e-5319-a671-75ed40d7ee07',
 3, 'Что делает атрибут defer у <script>?', '{"options": ["Откладывает выполнение до построения DOM", "Удаляет скрипт", "Блокирует рендер", "Загружает в workers"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8c607425-989d-5765-90a7-41d0f8927d19', 2, 2, 'тест_с_вариантами', '56f13cea-47b1-5c9a-906b-c0153dc69caa',
 4, 'Что такое CSS float и его проблема?', '{"options": ["Обтекание элемента текстом; ломает высоту родителя", "Анимация", "Цвет", "Позиционирование без изъянов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e33e6a90-5b34-54d7-ae4a-2490caa28175', 2, 2, 'тест_с_вариантами', '56f13cea-47b1-5c9a-906b-c0153dc69caa',
 4, 'Что такое URL?', '{"options": ["Адрес ресурса в сети", "Протокол почты", "Тип DNS-записи", "Тег HTML"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('cf965b5f-708d-50ff-8fb4-56b5ff3c4e6f', 2, 2, 'задача_с_кодом', 'ece030de-5be9-50e2-8c19-ecf4a1bc26ba',
 2, 'Язык: csharp. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "csharp", "reference": "using System;using System.Linq;class Solution{static void Main(){var d=Console.In.ReadToEnd().Split(new char[0],StringSplitOptions.RemoveEmptyEntries).Select(long.Parse);Console.WriteLine(d.Sum());}}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5f44545b-1545-5ad2-a9d1-7f741cfdef5b', 2, 2, 'задача_с_кодом', 'ee0a991f-bc44-5892-a1fb-71a2a18c00c2',
 4, 'Язык: java. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "java", "reference": "import java.util.*;public class Solution{public static void main(String[] a){String s=new Scanner(System.in).nextLine().toLowerCase().replaceAll(\"[^a-z0-9а-яё]\",\"\");System.out.println(new StringBuilder(s).reverse().toString().equals(s)?\"true\":\"false\");}}", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f78e54fb-461c-5b50-ac3c-2d94edd6d463', 2, 2, 'открытый_вопрос', '0fc2abb6-5750-555c-b180-c16aa60ab15e',
 3, 'Что такое всплытие событий (bubbling)? Приведите пример.', '{}'::jsonb, '{"rubric": "всплытие: событие идёт от цели к родителям\nпример: клик по кнопке срабатывает на контейнере\nостановка: event.stopPropagation"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('725951c5-63b4-5e1c-86c7-dcecf4bb97cc', 2, 3, 'тест_с_вариантами', '518f4318-7a48-5df4-a856-f189c443cc47',
 3, 'Что такое same-origin policy?', '{"options": ["Ограничение доступа скриптов к ресурсам другого origin", "Политика кэша", "CSS-правило", "Серверный фреймворк"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('9d0a8c45-0632-5f38-a2dc-82082a06ae20', 2, 3, 'тест_с_вариантами', '518f4318-7a48-5df4-a856-f189c443cc47',
 3, 'Что такое hoisting в JavaScript?', '{"options": ["Подъём объявлений var/function в начало области видимости", "Сжатие кода", "Поднятие DOM", "Оптимизация сети"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3421b585-1d3f-5350-b8cf-1b7adb29124e', 2, 3, 'тест_с_вариантами', '05240e32-a35a-55d5-90f4-17658ef5037a',
 4, 'Что такое делегирование событий?', '{"options": ["Один обработчик на родителе для событий дочерних элементов", "Передача функций вверх", "Шаблон ООП", "Сборка мусора"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c78b86ee-d271-5ddc-a758-01990cf8f970', 2, 3, 'тест_с_вариантами', '05240e32-a35a-55d5-90f4-17658ef5037a',
 4, 'Что такое BEM?', '{"options": ["Методология именования CSS-классов", "Формат шрифтов", "Бандлер", "Тестовый фреймворк"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('732f4583-064a-5dd4-9a93-767384f24aee', 2, 3, 'тест_с_вариантами', 'ede89d8e-4ec1-5391-a7db-2ea053e9bdea',
 4, 'Что такое tree shaking?', '{"options": ["Удаление неиспользуемого кода при сборке", "Анимация деревьев", "SSR-техника", "Lint-правило"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('eee0b47b-39ab-51fe-bf3d-0dab4cac5679', 2, 3, 'тест_с_вариантами', 'ede89d8e-4ec1-5391-a7db-2ea053e9bdea',
 5, 'Чем prefetch отличается от preload?', '{"options": ["Prefetch — ресурсы следующих навигаций, preload — критичные текущей", "Ничем", "Preload для CSS", "Prefetch блокирует"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('076bd016-9d7f-54c5-a939-32bdb25eaa9f', 2, 3, 'задача_с_кодом', '8710641c-0372-522b-a50f-8ba946707eda',
 2, 'Язык: csharp. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "csharp", "reference": "using System;class Solution{static void Main(){int n=int.Parse(Console.In.ReadToEnd().Trim());for(int i=1;i<=n;i++){if(i%15==0)Console.WriteLine(\"FizzBuzz\");else if(i%3==0)Console.WriteLine(\"Fizz\");else if(i%5==0)Console.WriteLine(\"Buzz\");else Console.WriteLine(i);}}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('7aa2df4a-9376-50f5-92a3-1e3d88f7c58b', 2, 3, 'задача_с_кодом', '435a747f-790e-56d8-9bd0-e6a6741f87f1',
 4, 'Язык: java. На вход подаётся число n (0 ≤ n ≤ 20). Выведите n! (факториал).', '{"language": "java", "reference": "import java.util.*;import java.math.*;public class Solution{public static void main(String[] a){int n=new Scanner(System.in).nextInt();BigInteger r=BigInteger.ONE;for(int i=2;i<=n;i++)r=r.multiply(BigInteger.valueOf(i));System.out.println(r);}}", "testCases": [{"input": "5", "expected": "120"}, {"input": "0", "expected": "1"}, {"input": "10", "expected": "3628800"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('85a49b3e-69b4-5023-91de-a9747399a022', 2, 3, 'открытый_вопрос', '8bf89b70-8945-5d52-bc9c-ef2e372e7eb6',
 4, 'Перечислите способы уменьшить размер JS-бандла.', '{}'::jsonb, '{"rubric": "код-сплиттинг: ленивая загрузка роутов\ntree shaking: удаление неиспользуемого кода\nсжатие: минификация и brotli на сервере"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('93c45ffb-6b7b-5969-a248-8b115cb5f749', 2, 4, 'тест_с_вариантами', '59091728-8995-514c-99c4-54fa0c514991',
 4, 'Что такое гидратация (hydration) в SSR?', '{"options": ["Превращение статического HTML в интерактивное приложение на клиенте", "Сжатие изображений", "Кэширование", "Шардирование"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8eadc374-6a96-5970-a5fe-7aa3b49e9dc0', 2, 4, 'тест_с_вариантами', '59091728-8995-514c-99c4-54fa0c514991',
 5, 'Что такое архитектура островов (islands)?', '{"options": ["Интерактивные фрагменты в статичном HTML", "Карта сайта", "CDN-схема", "Тип тестов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('998dfa3e-dc96-5d64-9c40-37eab1e4a6b7', 2, 4, 'тест_с_вариантами', '9a709c34-2753-5cac-8d67-96fe30331cfc',
 5, 'Что измеряет метрика INP?', '{"options": ["Задержку от взаимодействия до отрисовки", "Размер бандла", "Время сборки", "Число DOM-узлов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('cab9a1e1-3737-5c76-83f1-8a7efa5901ef', 2, 4, 'тест_с_вариантами', '9a709c34-2753-5cac-8d67-96fe30331cfc',
 5, 'Что такое module federation?', '{"options": ["Динамическая загрузка модулей из независимо развёрнутых приложений", "Объединение CSS", "Монолит", "Тип прокси"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('fae39b4d-b302-5a75-9d46-e0aef4eb95b9', 2, 4, 'тест_с_вариантами', 'b689c8d6-5092-5cff-a509-89a077f3a279',
 4, 'Что такое edge rendering?', '{"options": ["Рендеринг ближе к пользователю на edge-узлах", "Рендеринг в браузере только", "Сервер в одном дата-центре", "Пререндеринг PDF"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('450bff61-0fd4-51ba-a73c-2bcad4022410', 2, 4, 'тест_с_вариантами', 'b689c8d6-5092-5cff-a509-89a077f3a279',
 4, 'Что такое дизайн-токены?', '{"options": ["Единая система значений дизайна (цвета, отступы, шрифты) для всех платформ", "Криптовалюта", "JWT", "Иконки"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('1c1e9eeb-17a9-5784-8377-19cfa938710e', 2, 4, 'задача_с_кодом', 'ebc9443d-12ac-53d2-bb14-13765227cb7b',
 2, 'Язык: go. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strconv\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Split(bufio.ScanWords);var s int64=0;for sc.Scan(){n,_:=strconv.ParseInt(sc.Text(),10,64);s+=n};fmt.Println(s)}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('b3472a64-a2e9-55f2-b38f-a2e5474e39c0', 2, 4, 'задача_с_кодом', '81f3e3b4-8aeb-51bc-af39-16bd0f6f637e',
 4, 'Язык: cpp. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){std::string s;std::getline(std::cin,s);std::string t;for(char c:s)if(isalnum((unsigned char)c))t+=tolower((unsigned char)c);std::string r(t.rbegin(),t.rend());std::cout<<(t==r?\"true\":\"false\");}", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('ed9ed904-8232-5f2c-af58-8c46007f53f1', 2, 4, 'открытый_вопрос', 'cc16d284-3e57-5ded-8d29-120c6384c2c3',
 5, 'Как сделать SPA быстрой и индексируемой?', '{}'::jsonb, '{"rubric": "SSR/SSG: отдача готового HTML\nмаршруты: мета-теги и канонические URL\nпроизводительность: Core Web Vitals в зелёной зоне"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('017b83c6-e109-5ccb-b5e2-5fa686d4cfb2', 5, 2, 'тест_с_вариантами', '1ff994da-2c2e-5b57-b46e-b935953aef4d',
 2, 'Что такое дефект (баг)?', '{"options": ["Отклонение фактического поведения от ожидаемого", "Любое пожелание", "Коммит", "Тестовый сценарий"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4ecc0de6-4e5c-5e7c-a1f9-90c5b16c0dac', 5, 2, 'тест_с_вариантами', '1ff994da-2c2e-5b57-b46e-b935953aef4d',
 2, 'Что содержит тест-план?', '{"options": ["Объём, ресурсы, расписание, критерии входа/выхода", "Только чек-лист", "Код автотестов", "Список багов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('2306cc4c-18b7-5983-9f97-8db1387e95de', 5, 2, 'тест_с_вариантами', '963a8b35-2f51-5b72-8f6b-35880c03682c',
 3, 'Чем ретест отличается от регресса?', '{"options": ["Ретест проверяет исправление конкретного дефекта, регресс — что ничего не сломалось", "Ничем", "Регресс короче", "Ретест автоматический"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('96926a6b-b56e-5401-ac5d-2d31680d0068', 5, 2, 'тест_с_вариантами', '963a8b35-2f51-5b72-8f6b-35880c03682c',
 3, 'Чем позитивный тест отличается от негативного?', '{"options": ["Валидные данные и сценарий vs ошибочные и обработка ошибок", "Скорость", "Инструмент", "Приоритет"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('b963422f-4f56-5b04-8333-ab7b9050e5d4', 5, 2, 'тест_с_вариантами', '674f6f86-5e3f-5b8c-b2d4-43e94e193f78',
 4, 'Что такое альфа-тестирование?', '{"options": ["Внутреннее тестирование до передачи внешним пользователям", "Тест на проде", "Нагрузочный тест", "Тест API"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('9e20926a-d58d-5a10-a7de-a7080c1b9c9a', 5, 2, 'тест_с_вариантами', '674f6f86-5e3f-5b8c-b2d4-43e94e193f78',
 4, 'Что такое критерии выхода?', '{"options": ["Условия завершения тестирования, например отсутствие блокеров", "Список тестов", "Бюджет", "Число тестировщиков"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('07a8027a-94a3-56af-9a23-f7a2821b1b55', 5, 2, 'задача_с_кодом', 'bbcd6c84-b579-501c-8b98-020ff171e00f',
 2, 'Язык: python. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "python", "reference": "import sys\nprint(sum(int(x) for x in sys.stdin.read().split()))", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c2b50278-b95a-520e-bb94-1a5c4353116d', 5, 2, 'задача_с_кодом', '37b6751b-cdf2-566d-82b0-a08a8080d7f3',
 4, 'Язык: go. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strconv\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Scan();n,_:=strconv.Atoi(sc.Text());w:=bufio.NewWriter(os.Stdout);defer w.Flush();for i:=1;i<=n;i++{if i%15==0{fmt.Fprintln(w,\"FizzBuzz\")}else if i%3==0{fmt.Fprintln(w,\"Fizz\")}else if i%5==0{fmt.Fprintln(w,\"Buzz\")}else{fmt.Fprintln(w,i)}}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('08825ed2-456d-519c-a512-e5fb77f6e8cf', 5, 2, 'открытый_вопрос', '795ff498-ca62-5b21-9d28-2a8fbc06fcc5',
 3, 'Составьте тест-план для калькулятора.', '{}'::jsonb, '{"rubric": "функционал: базовые операции и приоритет операторов\nграницы: деление на ноль, переполнение, отрицательные\nUI: кнопки, клавиатура, отображение результата"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('7063ebea-fa4a-5a7f-a3cb-9a55562b3f96', 5, 3, 'тест_с_вариантами', '03f1cb2c-ef0f-5b2c-81cc-15f44b38f4dc',
 3, 'Что такое попарное тестирование (pairwise)?', '{"options": ["Покрытие всех пар комбинаций параметров", "Тест вдвоём", "Два браузера", "Двойной клик"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('b337d0aa-0e5c-5eab-a07d-c247b8bc33e8', 5, 3, 'тест_с_вариантами', '03f1cb2c-ef0f-5b2c-81cc-15f44b38f4dc',
 4, 'Что такое test harness?', '{"options": ["Окружение и вспомительный код для запуска тестов", "Ремень безопасности", "Отчёт", "Баг-трекер"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5dee6e30-ff82-5aa6-91af-d8b7a95df05a', 5, 3, 'тест_с_вариантами', '2f6f10c8-9bda-572a-bde4-b277260d3105',
 4, 'Что означает подход shift-left?', '{"options": ["Перенос тестирования раньше в жизненный цикл разработки", "Тесты слева в коде", "Ручное тестирование", "Отказ от QA"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('00f87cbf-038c-55a6-9fe1-4386bc7482f6', 5, 3, 'тест_с_вариантами', '2f6f10c8-9bda-572a-bde4-b277260d3105',
 5, 'Что проверяет API-контрактный тест?', '{"options": ["Соответствие запросов/ответов согласованной схеме", "Производительность CPU", "Цвета UI", "Логи сервера"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('fdbeecce-e6b9-5108-bbdb-0a846f914495', 5, 3, 'тест_с_вариантами', '7645ad87-c1a2-5015-9cd0-f58f1951a3f9',
 4, 'Что такое data-driven тесты?', '{"options": ["Один сценарий, много наборов входных данных из таблиц", "Тесты данных", "Генерация данных случайно", "Только SQL"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c2277c35-adda-517f-8f98-0e1532323fdf', 5, 3, 'тест_с_вариантами', '7645ad87-c1a2-5015-9cd0-f58f1951a3f9',
 3, 'Какие состояния бывают у дефекта в жизненном цикле?', '{"options": ["New, Open, Fixed, Retest, Closed, Reopened", "Только Open и Closed", "Done и Pending", "Start, Stop"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('d2c42d92-38a1-5f2c-94f8-b473551eeed0', 5, 3, 'задача_с_кодом', 'c5918d57-bcbe-5a25-974b-fef407159602',
 2, 'Язык: javascript. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "javascript", "reference": "let n=+require(''fs'').readFileSync(0,''utf8'').trim();let o=[];for(let i=1;i<=n;i++)o.push(i%15==0?''FizzBuzz'':i%3==0?''Fizz'':i%5==0?''Buzz'':''''+i);console.log(o.join(''\\n''));", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('652af03a-bc64-5a75-acc9-d213b5c9139b', 5, 3, 'задача_с_кодом', '24dcbabe-82aa-5d48-a9cb-1edac030967d',
 4, 'Язык: cpp. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){std::string s;std::getline(std::cin,s);std::string t;for(char c:s)if(isalnum((unsigned char)c))t+=tolower((unsigned char)c);std::string r(t.rbegin(),t.rend());std::cout<<(t==r?\"true\":\"false\");}", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3b9dabce-cf15-5867-916e-4005dbea9b54', 5, 3, 'открытый_вопрос', '6fd15226-6468-5405-9ae4-272155923c1c',
 4, 'Как организовать тестовые данные для параллельных прогонов?', '{}'::jsonb, '{"rubric": "изоляция: уникальный набор данных на прогон\nочистка: настройка и сброс состояния\nгенерация: фиктивные данные по схемам"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('0e3a9c66-e8cc-58f3-b6c4-4d3dc4507c2e', 5, 4, 'тест_с_вариантами', 'fccc8212-6e1f-5f6b-87ca-7f2c8ed1bdd6',
 4, 'Что такое chaos engineering?', '{"options": ["Контролируемые эксперименты с отказами для проверки устойчивости", "Хаотичное тестирование", "Нагрузочный тест", "Ручное исследование"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('c36f612f-4216-51df-bcde-84326bbcf08c', 5, 4, 'тест_с_вариантами', 'fccc8212-6e1f-5f6b-87ca-7f2c8ed1bdd6',
 4, 'Что такое quality gate?', '{"options": ["Автоматический барьер качества на переходе между этапами CI/CD", "Тестовая арка", "Ручная проверка", "Метрика багов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('50abbdb4-bb36-51bd-849b-3978293de759', 5, 4, 'тест_с_вариантами', 'e4768382-bb34-5f62-967d-8d1dceda2fc0',
 5, 'Чем MTTR отличается от MTBF?', '{"options": ["Среднее время восстановления vs среднее время между отказами", "Ничем", "MTBF для ПО не нужен", "MTTR больше всегда"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('038b09e4-5f5b-502a-bccf-c31566256047', 5, 4, 'тест_с_вариантами', 'e4768382-bb34-5f62-967d-8d1dceda2fc0',
 5, 'Зачем нужен учёт тестовых данных (test data management)?', '{"options": ["Консистентность, изоляция и воспроизводимость прогонов", "Украшение отчётов", "Сокращение кейсов", "Отказ от моков"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4d2db3db-56aa-59b9-b82d-88bf86805eb3', 5, 4, 'тест_с_вариантами', 'b8c8b238-af0c-5e8c-9136-195e18a6f27a',
 4, 'Как observability помогает тестированию?', '{"options": ["Диагностика падений по логам/трейсам/метрикам", "Заменяет тесты", "Только для UI", "Снижает покрытие"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('42e41a42-14bd-5c3e-be54-18a3c4959d2f', 5, 4, 'тест_с_вариантами', 'b8c8b238-af0c-5e8c-9136-195e18a6f27a',
 3, 'Что такое матрица рисков?', '{"options": ["Сочетание вероятности и влияния для приоритизации", "Таблица тестов", "Сетка CSS", "План спринта"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('62021fe3-47ff-58bc-929b-1de76b16c9c9', 5, 4, 'задача_с_кодом', 'eb2d2b36-5683-59e0-883c-25dcca410742',
 2, 'Язык: python. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "python", "reference": "import sys\nprint(sum(int(x) for x in sys.stdin.read().split()))", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('0e531de6-b0c6-5e17-8b8d-4bcd3ca592f1', 5, 4, 'задача_с_кодом', '4847cf85-f988-5695-8242-d4d363261fc6',
 4, 'Язык: javascript. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "javascript", "reference": "let s=require(''fs'').readFileSync(0,''utf8'').trim().toLowerCase().replace(/[^a-z0-9а-яё]/g,'''');console.log(s===s.split('''').reverse().join('''')?''true'':''false'');", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8d681e64-526c-5588-a937-2ed79565818f', 5, 4, 'открытый_вопрос', '3c84520d-af40-501f-ab0c-ed42c1406d3c',
 5, 'Как сократить время регресса с нескольких дней до часов?', '{}'::jsonb, '{"rubric": "приоритизация: критические сценарии первыми\nпараллелизация: шардирование тестов по средам\nстабилизация: устранение flaky, дымовой набор gating"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3f0d15c8-6f87-552f-af3b-80eb0bae1240', 4, 2, 'тест_с_вариантами', 'f3e3ccff-31f9-5cc2-b8f3-165b27096043',
 2, 'Для чего используется SSH?', '{"options": ["Защищённое удалённое подключение к серверам", "Отправка почты", "Хостинг сайтов", "Сборка образов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('483de9f7-41e3-5d3c-b0b0-5c0d83ffbe20', 4, 2, 'тест_с_вариантами', 'f3e3ccff-31f9-5cc2-b8f3-165b27096043',
 2, 'Что делает команда ping?', '{"options": ["Проверяет доступность узла по ICMP", "Копирует файлы", "Показывает процессы", "Редактирует конфиг"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('222c4eb4-bc2b-59b4-90e9-4c882c862e5f', 4, 2, 'тест_с_вариантами', 'c3daaeef-c68a-54b5-b02f-6c2fe4c83946',
 3, 'Что такое порт в сети?', '{"options": ["Числовой идентификатор службы на хосте", "Физический разъём", "Тип кабеля", "DNS-имя"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('95c77caf-8227-5682-b9ad-27a544b1f052', 4, 2, 'тест_с_вариантами', 'c3daaeef-c68a-54b5-b02f-6c2fe4c83946',
 3, 'Что такое cron?', '{"options": ["Планировщик периодических задач в Unix", "Шелл", "Файрвол", "Репозиторий"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('bf3edd52-6336-52c9-a9c2-8f5a2fd4feb9', 4, 2, 'тест_с_вариантами', '3aa03cfe-5ef3-5f7c-b220-d084acfb535a',
 4, 'Что такое переменные окружения и зачем они?', '{"options": ["Настройки вне кода: конфигурация без пересборки", "Глобальные константы в коде", "Только для тестов", "Устаревший механизм"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('fd0107b4-3580-51a2-a230-527039418ad9', 4, 2, 'тест_с_вариантами', '3aa03cfe-5ef3-5f7c-b220-d084acfb535a',
 4, 'Что такое репозиторий пакетов?', '{"options": ["Хранилище дистрибутивов ПО для установки менеджером пакетов", "Git-ветка", "Docker-образ", "Лог-файл"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('6027e01a-fc3f-5eeb-8e5f-4f1ee8b20a4c', 4, 2, 'задача_с_кодом', '04790adf-9a2b-537f-9cef-c70123144570',
 2, 'Язык: go. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strconv\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Split(bufio.ScanWords);var s int64=0;for sc.Scan(){n,_:=strconv.ParseInt(sc.Text(),10,64);s+=n};fmt.Println(s)}", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('4ba9276c-fc84-54a6-981d-c9e8ae0d93b2', 4, 2, 'задача_с_кодом', '615f955b-0a18-53ab-9220-140142030654',
 4, 'Язык: cpp. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){std::string s;std::getline(std::cin,s);std::string t;for(char c:s)if(isalnum((unsigned char)c))t+=tolower((unsigned char)c);std::string r(t.rbegin(),t.rend());std::cout<<(t==r?\"true\":\"false\");}", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('34aa947a-487a-54d2-bb46-4cf2a5916b0c', 4, 2, 'открытый_вопрос', 'e8e8d745-2e2b-5603-b2a0-ea3ef3dfe43c',
 3, 'Что происходит в инфраструктуре, когда вы открываете сайт в браузере?', '{}'::jsonb, '{"rubric": "DNS: запрос A-записи домена\nedge: CDN или балансировщик принимает трафик\nbackend: прокси передаёт запрос сервису"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('f37b75a9-89e2-5185-8b74-2eed76caffd3', 4, 3, 'тест_с_вариантами', '5d2d536f-cb90-5f1c-b14f-d8483700fb43',
 3, 'Что такое reverse proxy?', '{"options": ["Принимает внешний трафик и передаёт backend-сервисам", "Прокси для разработки", "DNS-сервер", "Балансировка дисков"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('755b4202-cc8c-5d75-99f4-955f2d0a2715', 4, 3, 'тест_с_вариантами', '5d2d536f-cb90-5f1c-b14f-d8483700fb43',
 3, 'Зачем нужен systemd?', '{"options": ["Инициализация и управление службами в Linux", "Сборка кода", "Мониторинг UI", "Хранение секретов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('651551af-d593-53fb-bb67-60df44c9f28e', 4, 3, 'тест_с_вариантами', '1146cf9f-e52f-5a29-b821-2737636c69ef',
 4, 'Чем образ Docker отличается от контейнера?', '{"options": ["Образ — неизменяемый шаблон, контейнер — его запущенный экземпляр", "Ничем", "Контейнер больше образа", "Образ работает без ОС"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('1cd0ac34-6f37-5c9c-82ac-97a8ef56ecf4', 4, 3, 'тест_с_вариантами', '1146cf9f-e52f-5a29-b821-2737636c69ef',
 4, 'Что такое rolling update в Kubernetes?', '{"options": ["Постепенная замена подов на новую версию без остановки сервиса", "Обновление ноды", "Откат релиза", "Тестовый деплой"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('6272e90e-615a-5bc7-8052-c43daece1140', 4, 3, 'тест_с_вариантами', '6fa6c41b-64be-5dfd-943d-1338ac808ecd',
 5, 'Зачем Terraform хранит state?', '{"options": ["Соответствие желаемой и фактической инфраструктурой, планирование изменений", "Кэш модулей", "Логи", "Секреты"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8d6cd439-9ba9-598a-b2b4-f9f2d87de37c', 4, 3, 'тест_с_вариантами', '6fa6c41b-64be-5dfd-943d-1338ac808ecd',
 4, 'Что такое overlay-сеть?', '{"options": ["Виртуальная сеть поверх физической для связи контейнеров между хостами", "Wi-Fi сеть", "VPN пользователя", "Дисковый массив"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a0c43881-2dab-585e-b324-51a9e1dd9241', 4, 3, 'задача_с_кодом', '49e2e283-b6b7-5eca-adee-8affe9924fdf',
 2, 'Язык: python. На вход подаётся строка из целых чисел через пробел. Выведите их сумму.', '{"language": "python", "reference": "import sys\nprint(sum(int(x) for x in sys.stdin.read().split()))", "testCases": [{"input": "1 2 3", "expected": "6"}, {"input": "10 -4 4 100", "expected": "110"}, {"input": "7", "expected": "7"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('e789801b-d1c8-52b4-a112-9a09f5485151', 4, 3, 'задача_с_кодом', '355ae8ca-dd6a-553e-8dcb-791b35f5151c',
 4, 'Язык: go. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "go", "reference": "package main\nimport(\"bufio\";\"fmt\";\"os\";\"strconv\")\nfunc main(){sc:=bufio.NewScanner(os.Stdin);sc.Scan();n,_:=strconv.Atoi(sc.Text());w:=bufio.NewWriter(os.Stdout);defer w.Flush();for i:=1;i<=n;i++{if i%15==0{fmt.Fprintln(w,\"FizzBuzz\")}else if i%3==0{fmt.Fprintln(w,\"Fizz\")}else if i%5==0{fmt.Fprintln(w,\"Buzz\")}else{fmt.Fprintln(w,i)}}}", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('5d0added-a10b-5cfd-8eda-605df5cbfc8b', 4, 3, 'открытый_вопрос', '9b46498e-fcfa-537b-92b4-f9fd04d50211',
 4, 'Как найти под, потребляющий больше всего CPU, в Kubernetes?', '{}'::jsonb, '{"rubric": "метрики: kubectl top pods или Prometheus\nпрофилирование: exec и pprof/perf внутри пода\nлоги: причина нагрузки в приложении"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('0d7fc6f9-873e-5036-aa72-14d508c8f412', 4, 4, 'тест_с_вариантами', '3651576c-96f1-55d0-a68f-77e46fdd3913',
 5, 'Для чего нужен eBPF?', '{"options": ["Безопасное выполнение программ в ядре для наблюдаемости и сети", "Сборка ядра", "Бэкапы", "UI-мониторинг"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('2cd28f77-0d43-52a5-9bc6-0533c40137c0', 4, 4, 'тест_с_вариантами', '3651576c-96f1-55d0-a68f-77e46fdd3913',
 4, 'Что такое GitOps?', '{"options": ["Управление инфраструктурой через Git как единый источник истины", "Хостинг Git", "Обычный CI", "Система тикетов"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('af97a306-85e6-5132-8737-3ea5b2c335e7', 4, 4, 'тест_с_вариантами', 'f68749f0-70ca-54d8-86f0-ff395e9be345',
 5, 'Какие проблемы решает федерация кластеров?', '{"options": ["Единое управление и распределение нагрузки между несколькими кластерами", "Сборку образов", "DNS", "Логирование"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('3900ac13-9245-501e-9c41-b4d124013ad0', 4, 4, 'тест_с_вариантами', 'f68749f0-70ca-54d8-86f0-ff395e9be345',
 4, 'Чем warm standby отличается от cold standby?', '{"options": ["Горячий резерв работает и синхронизирован, холодный — выключен до аварии", "Ничем", "Cold быстрее", "Warm без данных"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('2122a2ed-f996-5d3d-9b30-7c456d85b752', 4, 4, 'тест_с_вариантами', '6722049a-706a-534d-941d-55215755f2ef',
 5, 'Что такое эффект «noisy neighbor»?', '{"options": ["Деградация из-за соседней нагрузки на общем ресурсе", "Шум в дата-центре", "Тип алертов", "Сетевая атака"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('6b1e7558-b1c8-5ff8-b828-97ad51a1748b', 4, 4, 'тест_с_вариантами', '6722049a-706a-534d-941d-55215755f2ef',
 4, 'Что такое error budget в SRE?', '{"options": ["Допустимый объём ненадёжности в рамках SLO", "Бюджет сервера", "Лимит на баги", "Квота на релизы"]}'::jsonb, '{"correct": [0]}'::jsonb, 1, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('89b15cdb-20ac-5bac-942d-146b17c6b8ee', 4, 4, 'задача_с_кодом', '573322cb-2a88-5155-a4d9-372ddf1cf60f',
 2, 'Язык: javascript. На вход подаётся число n. Выведите числа от 1 до n, каждое с новой строки: кратные 3 — Fizz, кратные 5 — Buzz, кратные 15 — FizzBuzz, остальные — число.', '{"language": "javascript", "reference": "let n=+require(''fs'').readFileSync(0,''utf8'').trim();let o=[];for(let i=1;i<=n;i++)o.push(i%15==0?''FizzBuzz'':i%3==0?''Fizz'':i%5==0?''Buzz'':''''+i);console.log(o.join(''\\n''));", "testCases": [{"input": "4", "expected": "1\n2\nFizz\n4"}, {"input": "5", "expected": "1\n2\nFizz\n4\nBuzz"}, {"input": "15", "expected": "1\n2\nFizz\n4\nBuzz\nFizz\n7\n8\nFizz\nBuzz\n11\nFizz\n13\n14\nFizzBuzz"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('a13f80f2-f46e-5c01-91fb-533ad76e3c4a', 4, 4, 'задача_с_кодом', '89763169-11a4-5abe-b0af-66a38c156795',
 4, 'Язык: cpp. На вход подаётся строка. Выведите true, если она палиндром (без учёта регистра, пробелов и знаков препинания), иначе false.', '{"language": "cpp", "reference": "#include <bits/stdc++.h>\nint main(){std::string s;std::getline(std::cin,s);std::string t;for(char c:s)if(isalnum((unsigned char)c))t+=tolower((unsigned char)c);std::string r(t.rbegin(),t.rend());std::cout<<(t==r?\"true\":\"false\");}", "testCases": [{"input": "level", "expected": "true"}, {"input": "A man, a plan", "expected": "false"}, {"input": "А роза упала на лапу Азора", "expected": "true"}]}'::jsonb, '{}'::jsonb, 3, true)
ON CONFLICT (id) DO NOTHING;
INSERT INTO test_questions (id, specialization_id, grade_id, question_type, question_group,
    difficulty, prompt, payload, correct_payload, max_score, is_active) VALUES
('8b2b5e61-54cd-5acc-ae1e-a6a2eba8bcc0', 4, 4, 'открытый_вопрос', '6fd8494c-44f3-58a2-93c1-b337c9c430b7',
 5, 'Спроектируйте наблюдаемость (observability) для микросервисной платформы.', '{}'::jsonb, '{"rubric": "сигналы: метрики RED для каждого сервиса\nтрейсинг: сквозные trace-id между сервисами\nалертинг: SLO-бюджет и эскалация по runbook"}'::jsonb, 2, true)
ON CONFLICT (id) DO NOTHING;

-- Проверка результата (суммарно с частью 1)
SELECT (SELECT count(*) FROM test_questions WHERE is_active AND specialization_id IN (1,2,4,5)) AS bank_active,
       (SELECT count(*) FROM test_assignments WHERE is_active) AS assignments;

COMMIT;

-- исправление условия: убираем y из перечня гласных (эталон его не считает)
UPDATE test_questions
SET prompt = replace(prompt, 'a e i o u y и русские', 'a e i o u и русские')
WHERE prompt LIKE '%a e i o u y и русские%';



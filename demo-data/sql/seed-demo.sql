-- ============================================================================
-- SEED DEMO: демо-данные платформы ФСП (кандидаты, компании, вакансии,
-- приглашения, отклики, достижения ФСП, задания, уведомления).
--
-- Все аккаунты демо-данных: пароль password123
--   работодатели: hr@techsphere.demo.ru, hr@severdata.demo.ru
--   кандидаты:    candidate1..candidate15@demo.ru
--
-- Свойства:
--   * скрипт ПЕРЕЗАПУСКАЕМЫЙ: перед загрузкой удаляет только свои строки
--     (все email-ы @demo.ru), боевые/чужие данные не трогает;
--   * все INSERT-ы идемпотентны (ON CONFLICT DO NOTHING);
--   * выполняется в одной транзакции: при ошибке не оставляет частичных данных;
--   * требования: применён init.sql (справочники) и заполнен банк вопросов
--     (seed_tests.sql / генерация), иначе сессии не создадутся.
--
-- Запуск:
--   psql -d fsp_platform -f seed-demo.sql
--   или в docker: docker exec -i fsp_db psql -U postgres -d fsp_platform -f /seed/seed-demo.sql
-- ============================================================================

BEGIN;

-- ---- Защитные проверки окружения (необязательны, но спасают от «тихой» поломки) ----
DO $$
BEGIN
    IF (SELECT count(*) FROM statuses) < 10 THEN
        RAISE EXCEPTION 'Справочник statuses пуст: сначала выполните init.sql';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM test_questions WHERE is_active) THEN
        RAISE WARNING 'Активный банк вопросов пуст: тестовые сессии будут без ответов, подборка в выдаче не появится. Сначала запустите генерацию банка.';
    END IF;
END $$;

CREATE TEMP TABLE tmp_plan (session_id uuid PRIMARY KEY, candidate_id uuid, percent numeric, spec_id smallint, grade_id smallint) ON COMMIT DROP;

-- ============================================================================
-- Чистка предыдущей демо-загрузки (ТОЛЬКО @demo.ru)
-- ============================================================================
DELETE FROM notifications WHERE user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM vacancy_reports WHERE reporter_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM audit_log WHERE user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM grade_change_history WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM test_session_answers WHERE session_id IN (SELECT id FROM test_sessions WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru'));
DELETE FROM test_sessions WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM survey_answers WHERE session_id IN (SELECT id FROM survey_sessions WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru'));
DELETE FROM survey_sessions WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM candidate_tasks WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM employer_tasks WHERE company_id IN (SELECT id FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru'));
DELETE FROM invitations WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru') OR company_id IN (SELECT id FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru'));
DELETE FROM applications WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru') OR vacancy_id IN (SELECT id FROM vacancies WHERE company_id IN (SELECT id FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru')));
DELETE FROM vacancy_skills WHERE vacancy_id IN (SELECT id FROM vacancies WHERE company_id IN (SELECT id FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru')));
DELETE FROM vacancies WHERE company_id IN (SELECT id FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru'));
DELETE FROM ats_integrations WHERE company_id IN (SELECT id FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru'));
DELETE FROM employer_search_results WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru') OR request_id IN (SELECT id FROM employer_search_requests WHERE company_id IN (SELECT id FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru')));
DELETE FROM employer_search_requests WHERE company_id IN (SELECT id FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru'));
DELETE FROM companies WHERE owner_user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM fsp_achievements WHERE fsp_id IN (SELECT fsp_id FROM users WHERE email LIKE '%@demo.ru' AND fsp_id IS NOT NULL);
DELETE FROM candidate_skills WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM candidate_roles WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM candidate_categories WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM candidate_specializations WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM candidate_profiles WHERE user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM consent_log WHERE user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM auth_tokens WHERE user_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru');
DELETE FROM users WHERE email LIKE '%@demo.ru';

-- ============================================================================
-- Профили участников ФСП (FK users.fsp_id -> fsp_profiles)
-- ============================================================================
INSERT INTO fsp_profiles (fsp_id) VALUES
('45658ffa-7235-4de3-87c5-cfb7aed36e4a'),
('ce6a46b9-67bc-4716-b5b9-b9a90eb4da1f'),
('7232505f-15e1-4803-bb76-cffe4f4f588b'),
('d1aad7e1-7a28-4d70-bdb3-8685db4c200a'),
('68b36226-ddad-4647-af83-26c47ed1133e'),
('390fff4b-aad1-4bde-bd5a-87928417eb0f')
ON CONFLICT (fsp_id) DO NOTHING;

-- ============================================================================
-- Пользователи (пароль всех: password123)
-- ============================================================================
INSERT INTO users (id, email, password_hash, role, email_confirmed, is_active, fsp_id, fsp_linked_at, last_login_at) VALUES
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 'candidate1@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, '45658ffa-7235-4de3-87c5-cfb7aed36e4a', now() - interval '2 days', now() - interval '6 days'),
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 'candidate2@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, 'ce6a46b9-67bc-4716-b5b9-b9a90eb4da1f', now() - interval '2 days', now() - interval '3 days'),
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 'candidate3@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '8 days'),
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 'candidate4@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '7 days'),
('bb64c541-6009-459a-b0dd-6af0605c9593', 'candidate5@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, '7232505f-15e1-4803-bb76-cffe4f4f588b', now() - interval '2 days', now() - interval '1 days'),
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 'candidate6@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '2 days'),
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 'candidate7@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '9 days'),
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 'candidate8@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, 'd1aad7e1-7a28-4d70-bdb3-8685db4c200a', now() - interval '2 days', now() - interval '10 days'),
('9facf097-b4ae-42d4-8bb5-41e245a533e2', 'candidate9@demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '6 days'),
('96f0aeff-3796-463c-ba18-f1911e705f8f', 'candidate10@demo.ru', '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '6 days'),
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 'candidate11@demo.ru', '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, '68b36226-ddad-4647-af83-26c47ed1133e', now() - interval '2 days', now() - interval '6 days'),
('c7b775e8-9708-4501-baba-e4e5d8f94113', 'candidate12@demo.ru', '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '10 days'),
('373d6999-bcbc-4144-825a-818bb5f2b122', 'candidate13@demo.ru', '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, '390fff4b-aad1-4bde-bd5a-87928417eb0f', now() - interval '2 days', now() - interval '8 days'),
('1db1e48b-2145-441b-b86a-337483ba35f3', 'candidate14@demo.ru', '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '10 days'),
('0b89e5e4-2d3a-431d-973a-56aab3c625b0', 'candidate15@demo.ru', '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат',   true, true, NULL, NULL, now() - interval '8 days'),
('4b5703cb-ab7b-4bda-bdc8-2a6f46836a7d', 'hr@techsphere.demo.ru', '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'работодатель', true, true, NULL, NULL, now() - interval '1 day'),
('2ad78e7e-6359-4c94-87e0-f7d0a4cc24df', 'hr@severdata.demo.ru',  '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'работодатель', true, true, NULL, NULL, now() - interval '1 day')
ON CONFLICT (id) DO NOTHING;

-- ============================================================================
-- Профили кандидатов
-- ============================================================================
INSERT INTO candidate_profiles (user_id, last_name, first_name, city, about, experience_years, soft_skills, is_profile_public, profile_completeness, last_activity_at) VALUES
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 'Иванов',   'Пётр',   'Москва',           'Backend-разработчик: ASP.NET Core, PostgreSQL. Участник олимпиад ФСП.', 1.5, ARRAY['командная работа','коммуникация'], true, 64, now()),
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 'Смирнова', 'Анна',   'Москва',           'Frontend: React + TypeScript, люблю чистый UI.',                        1.0, ARRAY['командная работа','коммуникация'], true, 77, now() - interval '3 days'),
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 'Козлов',   'Дмитрий','Санкт-Петербург',  'QA: автотесты на Python, нагрузочное тестирование.',                    2.5, ARRAY['командная работа','коммуникация'], true, 64, now()),
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 'Гусев',    'Артём',  'Москва',           'QA: автотесты на Python, нагрузочное тестирование.',                    0.5, ARRAY['командная работа','коммуникация'], true, 79, now() - interval '5 days'),
('bb64c541-6009-459a-b0dd-6af0605c9593', 'Морозова', 'Елена',  'Казань',           'QA: автотесты на Python, нагрузочное тестирование.',                    3.0, ARRAY['командная работа','коммуникация'], true, 88, now() - interval '2 days'),
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 'Петров',   'Сергей', 'Москва',           'Frontend: React + TypeScript, люблю чистый UI.',                        0.8, ARRAY['командная работа','коммуникация'], true, 84, now() - interval '5 days'),
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 'Волкова',  'Мария',  'Новосибирск',      'Backend-разработчик: ASP.NET Core, PostgreSQL. Участник олимпиад ФСП.', 1.2, ARRAY['командная работа','коммуникация'], true, 82, now()),
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 'Соколов',  'Андрей', 'Москва',           'Frontend: React + TypeScript, люблю чистый UI.',                        4.0, ARRAY['командная работа','коммуникация'], true, 89, now() - interval '2 days'),
('9facf097-b4ae-42d4-8bb5-41e245a533e2', 'Лебедева', 'Ольга',  'Екатеринбург',     'Backend-разработчик: ASP.NET Core, PostgreSQL. Участник олимпиад ФСП.', 0.3, ARRAY['командная работа','коммуникация'], true, 70, now() - interval '4 days'),
('96f0aeff-3796-463c-ba18-f1911e705f8f', 'Никитин',  'Денис',  'Москва',           'QA: автотесты на Python, нагрузочное тестирование.',                    2.0, ARRAY['командная работа','коммуникация'], true, 67, now() - interval '3 days'),
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 'Захарова', 'Ирина',  'Санкт-Петербург',  'QA: автотесты на Python, нагрузочное тестирование.',                    1.8, ARRAY['командная работа','коммуникация'], true, 63, now() - interval '1 days'),
('c7b775e8-9708-4501-baba-e4e5d8f94113', 'Белов',    'Максим', 'Москва',           'DevOps: Kubernetes, observability. Автоматизирую всё.',                 0.6, ARRAY['командная работа','коммуникация'], true, 78, now() - interval '1 days'),
('373d6999-bcbc-4144-825a-818bb5f2b122', 'Тарасова', 'Наталья','Нижний Новгород',  'DevOps: Kubernetes, observability. Автоматизирую всё.',                 5.0, ARRAY['командная работа','коммуникация'], true, 75, now() - interval '3 days'),
('1db1e48b-2145-441b-b86a-337483ba35f3', 'Федоров',  'Игорь',  'Москва',           'QA: автотесты на Python, нагрузочное тестирование.',                    1.1, ARRAY['командная работа','коммуникация'], true, 85, now() - interval '3 days'),
('0b89e5e4-2d3a-431d-973a-56aab3c625b0', 'Кузнецова','Юлия',   'Самара',           'QA: автотесты на Python, нагрузочное тестирование.',                    0.4, ARRAY['командная работа','коммуникация'], true, 65, now() - interval '1 days')
ON CONFLICT (user_id) DO NOTHING;

-- ============================================================================
-- Стек и роли кандидатов
-- ============================================================================
INSERT INTO candidate_skills (candidate_id, skill_id, proficiency, years_exp) VALUES
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 9, 3, 2.5), ('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 13, 4, 2.3),
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 10, 4, 2.2), ('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 3, 5, 2.9),
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 12, 3, 2.1), ('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 11, 2, 1.7),
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 8, 3, 1.2),
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 4, 4, 2.9), ('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 5, 2, 1.6),
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 7, 5, 1.5), ('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 6, 5, 0.8),
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 11, 2, 3.0), ('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 9, 5, 0.9),
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 1, 4, 2.0), ('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 2, 2, 0.5),
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 9, 2, 2.7), ('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 8, 5, 0.9),
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 4, 4, 2.9), ('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 14, 4, 1.7),
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 1, 5, 1.3), ('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 11, 3, 0.8),
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 10, 4, 2.4),
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 14, 3, 2.9), ('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 9, 4, 0.9),
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 13, 2, 2.4), ('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 8, 4, 2.9),
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 1, 3, 1.4), ('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 3, 3, 1.8),
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 10, 4, 2.1),
('bb64c541-6009-459a-b0dd-6af0605c9593', 4, 3, 1.0), ('bb64c541-6009-459a-b0dd-6af0605c9593', 9, 5, 1.4),
('bb64c541-6009-459a-b0dd-6af0605c9593', 8, 2, 3.0), ('bb64c541-6009-459a-b0dd-6af0605c9593', 14, 4, 1.7),
('bb64c541-6009-459a-b0dd-6af0605c9593', 2, 5, 2.5), ('bb64c541-6009-459a-b0dd-6af0605c9593', 11, 4, 2.9),
('bb64c541-6009-459a-b0dd-6af0605c9593', 10, 4, 0.7), ('bb64c541-6009-459a-b0dd-6af0605c9593', 3, 2, 1.1),
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 5, 2, 1.7), ('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 6, 4, 2.5),
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 4, 2, 2.6), ('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 13, 2, 2.8),
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 9, 4, 0.7), ('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 14, 5, 1.7),
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 8, 2, 2.3),
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 2, 5, 2.5), ('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 13, 3, 2.0),
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 1, 5, 2.1), ('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 11, 4, 0.9),
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 8, 2, 1.8), ('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 12, 3, 1.6),
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 5, 3, 1.2), ('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 13, 3, 2.4),
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 4, 4, 1.1), ('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 7, 5, 2.6),
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 8, 3, 1.8), ('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 9, 2, 2.7),
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 14, 3, 2.0),
('9facf097-b4ae-42d4-8bb5-41e245a533e2', 13, 5, 2.0), ('9facf097-b4ae-42d4-8bb5-41e245a533e2', 2, 2, 1.9),
('9facf097-b4ae-42d4-8bb5-41e245a533e2', 11, 4, 2.2), ('9facf097-b4ae-42d4-8bb5-41e245a533e2', 10, 5, 2.5),
('96f0aeff-3796-463c-ba18-f1911e705f8f', 8, 4, 0.6), ('96f0aeff-3796-463c-ba18-f1911e705f8f', 4, 2, 1.8),
('96f0aeff-3796-463c-ba18-f1911e705f8f', 14, 2, 2.4), ('96f0aeff-3796-463c-ba18-f1911e705f8f', 13, 2, 1.6),
('96f0aeff-3796-463c-ba18-f1911e705f8f', 9, 4, 1.6), ('96f0aeff-3796-463c-ba18-f1911e705f8f', 12, 5, 1.8),
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 9, 5, 0.8), ('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 13, 2, 1.5),
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 14, 4, 0.7), ('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 8, 3, 1.6),
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 12, 4, 0.9), ('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 11, 3, 2.9),
('c7b775e8-9708-4501-baba-e4e5d8f94113', 12, 3, 3.0), ('c7b775e8-9708-4501-baba-e4e5d8f94113', 11, 3, 0.9),
('c7b775e8-9708-4501-baba-e4e5d8f94113', 13, 5, 3.0), ('c7b775e8-9708-4501-baba-e4e5d8f94113', 9, 5, 1.3),
('c7b775e8-9708-4501-baba-e4e5d8f94113', 2, 4, 0.5), ('c7b775e8-9708-4501-baba-e4e5d8f94113', 3, 5, 1.6),
('c7b775e8-9708-4501-baba-e4e5d8f94113', 1, 2, 1.5),
('373d6999-bcbc-4144-825a-818bb5f2b122', 8, 2, 0.8), ('373d6999-bcbc-4144-825a-818bb5f2b122', 9, 3, 2.9),
('373d6999-bcbc-4144-825a-818bb5f2b122', 14, 2, 0.7), ('373d6999-bcbc-4144-825a-818bb5f2b122', 13, 4, 0.6),
('373d6999-bcbc-4144-825a-818bb5f2b122', 4, 4, 1.5),
('1db1e48b-2145-441b-b86a-337483ba35f3', 4, 2, 1.2), ('1db1e48b-2145-441b-b86a-337483ba35f3', 14, 3, 1.6),
('1db1e48b-2145-441b-b86a-337483ba35f3', 13, 2, 1.2), ('1db1e48b-2145-441b-b86a-337483ba35f3', 9, 2, 2.1),
('1db1e48b-2145-441b-b86a-337483ba35f3', 6, 2, 1.2), ('1db1e48b-2145-441b-b86a-337483ba35f3', 7, 2, 1.6),
('0b89e5e4-2d3a-431d-973a-56aab3c625b0', 13, 2, 1.8), ('0b89e5e4-2d3a-431d-973a-56aab3c625b0', 14, 3, 2.8),
('0b89e5e4-2d3a-431d-973a-56aab3c625b0', 9, 3, 1.2), ('0b89e5e4-2d3a-431d-973a-56aab3c625b0', 8, 3, 1.0)
ON CONFLICT DO NOTHING;

INSERT INTO candidate_roles (candidate_id, role_id)
SELECT id, 1 FROM users WHERE email LIKE 'candidate%@demo.ru'
ON CONFLICT DO NOTHING;
-- ============================================================================
-- Специализации кандидатов + пройденные тесты (завершённые сессии)
-- Время сессий согласовано: started_at < finished_at (= started + 18 минут)
-- ============================================================================
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 1, 2, 2, 'грейд_подтвержден', now() - interval '22 days', now() - interval '22 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('a230abce-d082-46f1-8804-db71df8e95bc', '8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 1, 2, 'завершен', 1, now() - interval '22 days', now() - interval '22 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('a230abce-d082-46f1-8804-db71df8e95bc', '8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 79.0, 1, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 1, 2, 2, 'первичное_тестирование', 'a230abce-d082-46f1-8804-db71df8e95bc', now() - interval '22 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 4, 3, 3, 'грейд_подтвержден', now() - interval '37 days', now() - interval '37 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('aeb1f0a9-5bf6-4ae8-981f-34d40f2931d6', '8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 4, 3, 'завершен', 1, now() - interval '37 days', now() - interval '37 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('aeb1f0a9-5bf6-4ae8-981f-34d40f2931d6', '8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 68, 4, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 4, 3, 3, 'первичное_тестирование', 'aeb1f0a9-5bf6-4ae8-981f-34d40f2931d6', now() - interval '37 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 2, 2, 2, 'грейд_подтвержден', now() - interval '11 days', now() - interval '11 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('93e79a3f-453d-42c7-9ef9-8d0fd4a52210', 'd0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 2, 2, 'завершен', 1, now() - interval '11 days', now() - interval '11 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('93e79a3f-453d-42c7-9ef9-8d0fd4a52210', 'd0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 90.0, 2, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 2, 2, 2, 'первичное_тестирование', '93e79a3f-453d-42c7-9ef9-8d0fd4a52210', now() - interval '11 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 1, 3, 3, 'грейд_подтвержден', now() - interval '18 days', now() - interval '18 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('e328007e-a1ab-4afe-9233-8449e6557e62', 'd0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 1, 3, 'завершен', 1, now() - interval '18 days', now() - interval '18 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('e328007e-a1ab-4afe-9233-8449e6557e62', 'd0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 87.0, 1, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 1, 3, 3, 'первичное_тестирование', 'e328007e-a1ab-4afe-9233-8449e6557e62', now() - interval '18 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 5, 2, 2, 'грейд_подтвержден', now() - interval '27 days', now() - interval '27 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('d34fde47-853c-4a40-a3a1-0f013a3dc080', '15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 5, 2, 'завершен', 1, now() - interval '27 days', now() - interval '27 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('d34fde47-853c-4a40-a3a1-0f013a3dc080', '15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 77.0, 5, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 5, 2, 2, 'первичное_тестирование', 'd34fde47-853c-4a40-a3a1-0f013a3dc080', now() - interval '27 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 1, 2, 2, 'грейд_подтвержден', now() - interval '15 days', now() - interval '15 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('b910fdcd-cb6b-4200-8e68-72c1fee4b7a0', '15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 1, 2, 'завершен', 1, now() - interval '15 days', now() - interval '15 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('b910fdcd-cb6b-4200-8e68-72c1fee4b7a0', '15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 60, 1, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 1, 2, 2, 'первичное_тестирование', 'b910fdcd-cb6b-4200-8e68-72c1fee4b7a0', now() - interval '15 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 5, 2, 2, 'грейд_подтвержден', now() - interval '20 days', now() - interval '20 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('ba17485f-decb-44c5-b2e8-710123e67c70', '5d7d6ead-e106-4de2-9ef8-3536fe48f268', 5, 2, 'завершен', 1, now() - interval '20 days', now() - interval '20 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('ba17485f-decb-44c5-b2e8-710123e67c70', '5d7d6ead-e106-4de2-9ef8-3536fe48f268', 81, 5, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 5, 2, 2, 'первичное_тестирование', 'ba17485f-decb-44c5-b2e8-710123e67c70', now() - interval '20 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 1, 2, 2, 'грейд_подтвержден', now() - interval '33 days', now() - interval '33 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('fc4e3d3d-8360-481c-9702-065e5fef8560', '5d7d6ead-e106-4de2-9ef8-3536fe48f268', 1, 2, 'завершен', 1, now() - interval '33 days', now() - interval '33 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('fc4e3d3d-8360-481c-9702-065e5fef8560', '5d7d6ead-e106-4de2-9ef8-3536fe48f268', 74, 1, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 1, 2, 2, 'первичное_тестирование', 'fc4e3d3d-8360-481c-9702-065e5fef8560', now() - interval '33 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('bb64c541-6009-459a-b0dd-6af0605c9593', 5, 2, 2, 'грейд_подтвержден', now() - interval '20 days', now() - interval '20 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('e3809b56-b989-43de-9673-93674f91c54d', 'bb64c541-6009-459a-b0dd-6af0605c9593', 5, 2, 'завершен', 1, now() - interval '20 days', now() - interval '20 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('e3809b56-b989-43de-9673-93674f91c54d', 'bb64c541-6009-459a-b0dd-6af0605c9593', 90.0, 5, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('bb64c541-6009-459a-b0dd-6af0605c9593', 5, 2, 2, 'первичное_тестирование', 'e3809b56-b989-43de-9673-93674f91c54d', now() - interval '20 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('bb64c541-6009-459a-b0dd-6af0605c9593', 1, 2, 2, 'грейд_подтвержден', now() - interval '26 days', now() - interval '26 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('22f4e34c-f6cb-4385-ba55-e615d2239c35', 'bb64c541-6009-459a-b0dd-6af0605c9593', 1, 2, 'завершен', 1, now() - interval '26 days', now() - interval '26 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('22f4e34c-f6cb-4385-ba55-e615d2239c35', 'bb64c541-6009-459a-b0dd-6af0605c9593', 91.0, 1, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('bb64c541-6009-459a-b0dd-6af0605c9593', 1, 2, 2, 'первичное_тестирование', '22f4e34c-f6cb-4385-ba55-e615d2239c35', now() - interval '26 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 2, 2, 2, 'грейд_подтвержден', now() - interval '20 days', now() - interval '20 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('1e39bf45-cde1-4315-a52d-88cc1b2cb096', '0bbb5b82-f302-4f44-b05a-a10d282dfcff', 2, 2, 'завершен', 1, now() - interval '20 days', now() - interval '20 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('1e39bf45-cde1-4315-a52d-88cc1b2cb096', '0bbb5b82-f302-4f44-b05a-a10d282dfcff', 75, 2, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 2, 2, 2, 'первичное_тестирование', '1e39bf45-cde1-4315-a52d-88cc1b2cb096', now() - interval '20 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 5, 1, 1, 'грейд_подтвержден', now() - interval '30 days', now() - interval '30 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('613987dd-dcbc-406f-8d10-4e7f671d8f63', '0bbb5b82-f302-4f44-b05a-a10d282dfcff', 5, 1, 'завершен', 1, now() - interval '30 days', now() - interval '30 days' + interval '18 minutes', true, 1) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('613987dd-dcbc-406f-8d10-4e7f671d8f63', '0bbb5b82-f302-4f44-b05a-a10d282dfcff', 52, 5, 1);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('0bbb5b82-f302-4f44-b05a-a10d282dfcff', 5, 1, 1, 'первичное_тестирование', '613987dd-dcbc-406f-8d10-4e7f671d8f63', now() - interval '30 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 1, 3, 3, 'грейд_подтвержден', now() - interval '10 days', now() - interval '10 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('347605a6-ddb0-482e-b49b-3a8a31a543a9', 'd67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 1, 3, 'завершен', 1, now() - interval '10 days', now() - interval '10 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('347605a6-ddb0-482e-b49b-3a8a31a543a9', 'd67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 69.0, 1, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 1, 3, 3, 'первичное_тестирование', '347605a6-ddb0-482e-b49b-3a8a31a543a9', now() - interval '10 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 4, 2, 2, 'грейд_подтвержден', now() - interval '36 days', now() - interval '36 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('719ecd35-2354-45b6-be76-1d7188d3b10f', 'd67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 4, 2, 'завершен', 1, now() - interval '36 days', now() - interval '36 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('719ecd35-2354-45b6-be76-1d7188d3b10f', 'd67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 82.0, 4, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('d67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 4, 2, 2, 'первичное_тестирование', '719ecd35-2354-45b6-be76-1d7188d3b10f', now() - interval '36 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 2, 2, 2, 'грейд_подтвержден', now() - interval '37 days', now() - interval '37 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('b2a5df02-edae-4d2f-a471-cd8bdf5f0eac', '1ed8c99f-867c-44ee-ade3-36fe96e774bd', 2, 2, 'завершен', 1, now() - interval '37 days', now() - interval '37 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('b2a5df02-edae-4d2f-a471-cd8bdf5f0eac', '1ed8c99f-867c-44ee-ade3-36fe96e774bd', 97, 2, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 2, 2, 2, 'первичное_тестирование', 'b2a5df02-edae-4d2f-a471-cd8bdf5f0eac', now() - interval '37 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 5, 2, 2, 'грейд_подтвержден', now() - interval '19 days', now() - interval '19 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('bcd1aa1c-e9d3-4b58-b34a-58e56970fca3', '1ed8c99f-867c-44ee-ade3-36fe96e774bd', 5, 2, 'завершен', 1, now() - interval '19 days', now() - interval '19 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('bcd1aa1c-e9d3-4b58-b34a-58e56970fca3', '1ed8c99f-867c-44ee-ade3-36fe96e774bd', 97, 5, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('1ed8c99f-867c-44ee-ade3-36fe96e774bd', 5, 2, 2, 'первичное_тестирование', 'bcd1aa1c-e9d3-4b58-b34a-58e56970fca3', now() - interval '19 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('9facf097-b4ae-42d4-8bb5-41e245a533e2', 1, 1, 1, 'грейд_подтвержден', now() - interval '29 days', now() - interval '29 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('2334a2f7-18f8-472b-8eca-29e2cb78d851', '9facf097-b4ae-42d4-8bb5-41e245a533e2', 1, 1, 'завершен', 1, now() - interval '29 days', now() - interval '29 days' + interval '18 minutes', true, 1) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('2334a2f7-18f8-472b-8eca-29e2cb78d851', '9facf097-b4ae-42d4-8bb5-41e245a533e2', 51, 1, 1);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('9facf097-b4ae-42d4-8bb5-41e245a533e2', 1, 1, 1, 'первичное_тестирование', '2334a2f7-18f8-472b-8eca-29e2cb78d851', now() - interval '29 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('96f0aeff-3796-463c-ba18-f1911e705f8f', 5, 3, 3, 'грейд_подтвержден', now() - interval '20 days', now() - interval '20 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('a6fc06a8-b2d6-48b3-9f74-45018e71c1d6', '96f0aeff-3796-463c-ba18-f1911e705f8f', 5, 3, 'завершен', 1, now() - interval '20 days', now() - interval '20 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('a6fc06a8-b2d6-48b3-9f74-45018e71c1d6', '96f0aeff-3796-463c-ba18-f1911e705f8f', 70.0, 5, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('96f0aeff-3796-463c-ba18-f1911e705f8f', 5, 3, 3, 'первичное_тестирование', 'a6fc06a8-b2d6-48b3-9f74-45018e71c1d6', now() - interval '20 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('96f0aeff-3796-463c-ba18-f1911e705f8f', 4, 3, 3, 'грейд_подтвержден', now() - interval '39 days', now() - interval '39 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('47fb9211-6d3c-45a1-89aa-fb81450b0dba', '96f0aeff-3796-463c-ba18-f1911e705f8f', 4, 3, 'завершен', 1, now() - interval '39 days', now() - interval '39 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('47fb9211-6d3c-45a1-89aa-fb81450b0dba', '96f0aeff-3796-463c-ba18-f1911e705f8f', 64.0, 4, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('96f0aeff-3796-463c-ba18-f1911e705f8f', 4, 3, 3, 'первичное_тестирование', '47fb9211-6d3c-45a1-89aa-fb81450b0dba', now() - interval '39 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 5, 3, 3, 'грейд_подтвержден', now() - interval '9 days', now() - interval '9 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('5523714d-27ef-4440-b2ba-bcbdabb92970', '5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 5, 3, 'завершен', 1, now() - interval '9 days', now() - interval '9 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('5523714d-27ef-4440-b2ba-bcbdabb92970', '5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 91.0, 5, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 5, 3, 3, 'первичное_тестирование', '5523714d-27ef-4440-b2ba-bcbdabb92970', now() - interval '9 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 4, 3, 3, 'грейд_подтвержден', now() - interval '34 days', now() - interval '34 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('f3de0351-7205-4bea-920a-5bff28da59c6', '5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 4, 3, 'завершен', 1, now() - interval '34 days', now() - interval '34 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('f3de0351-7205-4bea-920a-5bff28da59c6', '5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 92.0, 4, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('5c93cff1-8d6a-4d80-b44a-e21dce4c8c39', 4, 3, 3, 'первичное_тестирование', 'f3de0351-7205-4bea-920a-5bff28da59c6', now() - interval '34 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('c7b775e8-9708-4501-baba-e4e5d8f94113', 4, 1, 1, 'грейд_подтвержден', now() - interval '17 days', now() - interval '17 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('6832ed73-1e50-4f48-bbd2-9baf7705008d', 'c7b775e8-9708-4501-baba-e4e5d8f94113', 4, 1, 'завершен', 1, now() - interval '17 days', now() - interval '17 days' + interval '18 minutes', true, 1) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('6832ed73-1e50-4f48-bbd2-9baf7705008d', 'c7b775e8-9708-4501-baba-e4e5d8f94113', 55.0, 4, 1);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('c7b775e8-9708-4501-baba-e4e5d8f94113', 4, 1, 1, 'первичное_тестирование', '6832ed73-1e50-4f48-bbd2-9baf7705008d', now() - interval '17 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('c7b775e8-9708-4501-baba-e4e5d8f94113', 1, 1, 1, 'грейд_подтвержден', now() - interval '5 days', now() - interval '5 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('e073c4c5-f237-4eb6-99b8-a6dc90517142', 'c7b775e8-9708-4501-baba-e4e5d8f94113', 1, 1, 'завершен', 1, now() - interval '5 days', now() - interval '5 days' + interval '18 minutes', true, 1) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('e073c4c5-f237-4eb6-99b8-a6dc90517142', 'c7b775e8-9708-4501-baba-e4e5d8f94113', 60, 1, 1);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('c7b775e8-9708-4501-baba-e4e5d8f94113', 1, 1, 1, 'первичное_тестирование', 'e073c4c5-f237-4eb6-99b8-a6dc90517142', now() - interval '5 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('373d6999-bcbc-4144-825a-818bb5f2b122', 4, 3, 3, 'грейд_подтвержден', now() - interval '36 days', now() - interval '36 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('9a85cae1-0443-4edf-aacd-d8c0ef2cc245', '373d6999-bcbc-4144-825a-818bb5f2b122', 4, 3, 'завершен', 1, now() - interval '36 days', now() - interval '36 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('9a85cae1-0443-4edf-aacd-d8c0ef2cc245', '373d6999-bcbc-4144-825a-818bb5f2b122', 93.0, 4, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('373d6999-bcbc-4144-825a-818bb5f2b122', 4, 3, 3, 'первичное_тестирование', '9a85cae1-0443-4edf-aacd-d8c0ef2cc245', now() - interval '36 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('373d6999-bcbc-4144-825a-818bb5f2b122', 5, 3, 3, 'грейд_подтвержден', now() - interval '12 days', now() - interval '12 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('4eded4bb-9fc5-432c-aeee-47f4a6a9841a', '373d6999-bcbc-4144-825a-818bb5f2b122', 5, 3, 'завершен', 1, now() - interval '12 days', now() - interval '12 days' + interval '18 minutes', true, 3) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('4eded4bb-9fc5-432c-aeee-47f4a6a9841a', '373d6999-bcbc-4144-825a-818bb5f2b122', 97, 5, 3);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('373d6999-bcbc-4144-825a-818bb5f2b122', 5, 3, 3, 'первичное_тестирование', '4eded4bb-9fc5-432c-aeee-47f4a6a9841a', now() - interval '12 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('1db1e48b-2145-441b-b86a-337483ba35f3', 5, 2, 2, 'грейд_подтвержден', now() - interval '23 days', now() - interval '23 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('185730a2-5fd0-4e57-af07-6948bb1aaba6', '1db1e48b-2145-441b-b86a-337483ba35f3', 5, 2, 'завершен', 1, now() - interval '23 days', now() - interval '23 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('185730a2-5fd0-4e57-af07-6948bb1aaba6', '1db1e48b-2145-441b-b86a-337483ba35f3', 69, 5, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('1db1e48b-2145-441b-b86a-337483ba35f3', 5, 2, 2, 'первичное_тестирование', '185730a2-5fd0-4e57-af07-6948bb1aaba6', now() - interval '23 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('1db1e48b-2145-441b-b86a-337483ba35f3', 2, 2, 2, 'грейд_подтвержден', now() - interval '18 days', now() - interval '18 days', false) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('dca518a5-c635-43f8-a29e-b2fd69e184b2', '1db1e48b-2145-441b-b86a-337483ba35f3', 2, 2, 'завершен', 1, now() - interval '18 days', now() - interval '18 days' + interval '18 minutes', true, 2) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('dca518a5-c635-43f8-a29e-b2fd69e184b2', '1db1e48b-2145-441b-b86a-337483ba35f3', 76, 2, 2);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('1db1e48b-2145-441b-b86a-337483ba35f3', 2, 2, 2, 'первичное_тестирование', 'dca518a5-c635-43f8-a29e-b2fd69e184b2', now() - interval '18 days' + interval '18 minutes');
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, last_test_at, last_grade_change_at, is_primary) VALUES
('0b89e5e4-2d3a-431d-973a-56aab3c625b0', 5, 1, 1, 'грейд_подтвержден', now() - interval '21 days', now() - interval '21 days', true) ON CONFLICT DO NOTHING;
INSERT INTO test_sessions (id, candidate_id, specialization_id, target_grade_id, status, attempt_no, started_at, finished_at, passed, resulting_grade_id) VALUES
('78980324-6e35-44fb-a03e-7b48a10e7998', '0b89e5e4-2d3a-431d-973a-56aab3c625b0', 5, 1, 'завершен', 1, now() - interval '21 days', now() - interval '21 days' + interval '18 minutes', true, 1) ON CONFLICT (id) DO NOTHING;
INSERT INTO tmp_plan (session_id, candidate_id, percent, spec_id, grade_id) VALUES ('78980324-6e35-44fb-a03e-7b48a10e7998', '0b89e5e4-2d3a-431d-973a-56aab3c625b0', 68, 5, 1);
INSERT INTO grade_change_history (candidate_id, specialization_id, old_grade_id, new_grade_id, reason, test_session_id, changed_at) VALUES
('0b89e5e4-2d3a-431d-973a-56aab3c625b0', 5, 1, 1, 'первичное_тестирование', '78980324-6e35-44fb-a03e-7b48a10e7998', now() - interval '21 days' + interval '18 minutes');

-- ============================================================================
-- Ответы на вопросы: стратифицированная выборка из банка (как в реальном тесте),
-- скор ответа масштабируется на процент кандидата -> данные для v_question_stats
-- ============================================================================
INSERT INTO test_session_answers (session_id, question_id, order_no, answer_payload, score, auto_graded, graded_at, answered_at)
SELECT p.session_id, q.id, row_number() OVER (PARTITION BY p.session_id ORDER BY random()),
       '{}'::jsonb,
       round(q.max_score * p.percent / 100, 2),
       (q.question_type <> 'открытый_вопрос'),
       now() - interval '30 days', now() - interval '30 days'
FROM tmp_plan p
JOIN LATERAL (
    SELECT q.id, q.max_score, q.question_type, q.difficulty
    FROM test_questions q
    WHERE q.specialization_id = p.spec_id AND q.grade_id = p.grade_id AND q.is_active
    ORDER BY random()
    LIMIT 10
) q ON true;

-- Итоги сессий: max_score и total_score из фактических ответов
UPDATE test_sessions ts
SET max_score = sub.mx, total_score = sub.tt
FROM (SELECT a.session_id, SUM(q.max_score) mx, SUM(a.score) tt
      FROM test_session_answers a JOIN test_questions q ON q.id = a.question_id
      GROUP BY a.session_id) sub
WHERE ts.id = sub.session_id;

-- Денормализация категорий — то, что видит работодатель в выдаче
INSERT INTO candidate_categories (candidate_id, category_id, is_primary, updated_at)
SELECT cs.candidate_id, cat.id, cs.is_primary, now()
FROM candidate_specializations cs
JOIN categories cat ON cat.specialization_id = cs.specialization_id AND cat.grade_id = cs.current_grade_id
JOIN users u ON u.id = cs.candidate_id AND u.email LIKE '%@demo.ru'
ON CONFLICT DO NOTHING;

-- ============================================================================
-- Компании работодателей
-- ============================================================================
INSERT INTO companies (id, owner_user_id, name, industry, description, website, contact_email, status, verified_at, verification_level) VALUES
('06373a81-cd75-4172-a2f0-0c2b726d6a42', '4b5703cb-ab7b-4bda-bdc8-2a6f46836a7d', '«ТехноСфера»', 'Финансовые технологии', 'Платёжные сервисы: 40 инженеров, CI/CD, код-ревью для всех.', 'techsphere.ru', 'hr@techsphere.demo.ru', 'активен', NULL, 0),
('6240f031-41e9-4455-8d5f-047430106823', '2ad78e7e-6359-4c94-87e0-f7d0a4cc24df', '«СеверДата»', 'Облачные решения', 'Облачная платформа данных: Kubernetes, стриминг, SRE-культура.', 'severdata.ru', 'hr@severdata.demo.ru', 'активен', NULL, 0)
ON CONFLICT (id) DO NOTHING;

-- ============================================================================
-- Вакансии (опубликованные, с обязательной вилкой зарплаты)
-- ============================================================================
INSERT INTO vacancies (id, company_id, title, description, specialization_id, grade_from_id, grade_to_id, salary_from, salary_to, employment_type, location, status, published_at) VALUES
('a4312a99-178d-4a15-84e7-e8ed8243ae48', '06373a81-cd75-4172-a2f0-0c2b726d6a42', 'Backend-разработчик (финтех)',        'Команда: Финансовые технологии. Стек: C#, ASP.NET Core, PostgreSQL, Docker. Гибрид 2/5.', 1, 2, 3, 180000, 240000, 'hybrid', 'Москва', 'активен', now() - interval '19 days'),
('f6c933fa-a53b-432b-be50-27f9ad400c14', '06373a81-cd75-4172-a2f0-0c2b726d6a42', 'Разработчик API',                     'Команда: Финансовые технологии. Стек: C#, .NET, SQL. Гибрид 2/5.',                        1, 2, 3, 150000, 190000, 'hybrid', 'Москва', 'активен', now() - interval '11 days'),
('62e4a176-abf5-4937-a5ff-bebf238254a7', '06373a81-cd75-4172-a2f0-0c2b726d6a42', 'Frontend-разработчик',                'Команда: Финансовые технологии. Стек: TypeScript, React. Гибрид 2/5.',                    2, 2, 3, 140000, 200000, 'hybrid', 'Москва', 'активен', now() - interval '6 days'),
('fa52fedd-5798-488e-84c1-05bad8d60f1f', '6240f031-41e9-4455-8d5f-047430106823', 'DevOps-инженер',                      'Команда: Облачные решения. Стек: Docker, Kubernetes, Linux. Гибрид 2/5.',                 4, 3, 4, 220000, 300000, 'hybrid', 'Москва', 'активен', now() - interval '14 days'),
('72369cee-0fef-4a7b-92d1-65822ff134ce', '6240f031-41e9-4455-8d5f-047430106823', 'QA-инженер (автотесты)',              'Команда: Облачные решения. Стек: Python, SQL, Git. Гибрид 2/5.',                          5, 2, 3, 130000, 170000, 'hybrid', 'Москва', 'активен', now() - interval '10 days'),
('da03cf58-6720-404b-86da-e29f03d8082d', '6240f031-41e9-4455-8d5f-047430106823', 'Backend-разработчик (data platform)', 'Команда: Облачные решения. Стек: C#, PostgreSQL, Docker, Kubernetes. Гибрид 2/5.',        1, 3, 4, 250000, 320000, 'hybrid', 'Москва', 'активен', now() - interval '18 days'),
('81c39d39-8662-41d8-bf64-4260a28700fc', '06373a81-cd75-4172-a2f0-0c2b726d6a42', 'Стажёр Backend',                      'Команда: Финансовые технологии. Стек: Python, SQL. Гибрид 2/5.',                          1, 1, 1, 60000,  90000,  'hybrid', 'Москва', 'активен', now() - interval '18 days')
ON CONFLICT (id) DO NOTHING;

INSERT INTO vacancy_skills (vacancy_id, skill_id) VALUES
('a4312a99-178d-4a15-84e7-e8ed8243ae48', 1), ('a4312a99-178d-4a15-84e7-e8ed8243ae48', 3),
('a4312a99-178d-4a15-84e7-e8ed8243ae48', 10), ('a4312a99-178d-4a15-84e7-e8ed8243ae48', 11),
('f6c933fa-a53b-432b-be50-27f9ad400c14', 1), ('f6c933fa-a53b-432b-be50-27f9ad400c14', 2),
('f6c933fa-a53b-432b-be50-27f9ad400c14', 9),
('62e4a176-abf5-4937-a5ff-bebf238254a7', 5), ('62e4a176-abf5-4937-a5ff-bebf238254a7', 6),
('fa52fedd-5798-488e-84c1-05bad8d60f1f', 11), ('fa52fedd-5798-488e-84c1-05bad8d60f1f', 12),
('fa52fedd-5798-488e-84c1-05bad8d60f1f', 14),
('72369cee-0fef-4a7b-92d1-65822ff134ce', 8), ('72369cee-0fef-4a7b-92d1-65822ff134ce', 9),
('72369cee-0fef-4a7b-92d1-65822ff134ce', 13),
('da03cf58-6720-404b-86da-e29f03d8082d', 1), ('da03cf58-6720-404b-86da-e29f03d8082d', 10),
('da03cf58-6720-404b-86da-e29f03d8082d', 11), ('da03cf58-6720-404b-86da-e29f03d8082d', 12),
('81c39d39-8662-41d8-bf64-4260a28700fc', 8), ('81c39d39-8662-41d8-bf64-4260a28700fc', 9)
ON CONFLICT DO NOTHING;

-- ============================================================================
-- Приглашения: полный набор статусов для демо статусной модели
-- ============================================================================
INSERT INTO invitations (id, company_id, candidate_id, vacancy_id, title, message, salary_from, salary_to, contact_method, status, sent_at, viewed_at, responded_at, expires_at) VALUES
('f764bafe-6333-4c72-8a8a-697e66f8a59c', '06373a81-cd75-4172-a2f0-0c2b726d6a42', '8a3559ef-d33c-4e29-90e2-a4eddca1cc47', NULL, 'Приглашение: Backend-разработка',  'Приглашаем в команду. Стек и условия во вложении.', 180000, 240000, 'почта', 'отправлено', now() - interval '4 days', NULL, NULL, now() + interval '10 days'),
('e0ce362f-c564-443d-8a9f-429316320849', '06373a81-cd75-4172-a2f0-0c2b726d6a42', 'd0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', NULL, 'Приглашение: Frontend-разработка', 'Приглашаем в команду. Стек и условия во вложении.', 180000, 240000, 'почта', 'просмотрено', now() - interval '4 days', now() - interval '3 days', NULL, now() + interval '10 days'),
('c8b2bfc0-d673-40fe-aa24-eb8099d141ab', '06373a81-cd75-4172-a2f0-0c2b726d6a42', '15ca67e9-7768-4b1b-8be3-a6d567f09ba3', NULL, 'Приглашение: Тестирование (QA)',   'Приглашаем в команду. Стек и условия во вложении.', 180000, 240000, 'почта', 'принято', now() - interval '4 days', now() - interval '3 days', now() - interval '2 days', now() + interval '10 days'),
('040a2915-cead-4020-9b90-928a37e582b0', '06373a81-cd75-4172-a2f0-0c2b726d6a42', '5d7d6ead-e106-4de2-9ef8-3536fe48f268', NULL, 'Приглашение: Тестирование (QA)',   'Приглашаем в команду. Стек и условия во вложении.', 140000, 180000, 'почта', 'отклонено', now() - interval '4 days', now() - interval '3 days', now() - interval '2 days', now() + interval '10 days'),
('d0256ac7-b02c-427d-8376-f245c9c4e23b', '6240f031-41e9-4455-8d5f-047430106823', 'bb64c541-6009-459a-b0dd-6af0605c9593', NULL, 'Приглашение: Тестирование (QA)',   'Приглашаем в команду. Стек и условия во вложении.', 180000, 240000, 'почта', 'принято', now() - interval '4 days', now() - interval '3 days', now() - interval '2 days', now() + interval '10 days'),
('187d90f6-b023-494f-9b3b-0ca7cb72c20e', '06373a81-cd75-4172-a2f0-0c2b726d6a42', '1ed8c99f-867c-44ee-ade3-36fe96e774bd', NULL, 'Приглашение: Frontend-разработка', 'Приглашаем в команду. Стек и условия во вложении.', 180000, 240000, 'почта', 'истекло', now() - interval '4 days', NULL, NULL, now() + interval '10 days')
ON CONFLICT (id) DO NOTHING;

-- ============================================================================
-- Отклики кандидатов на вакансии
-- ============================================================================
INSERT INTO applications (id, vacancy_id, candidate_id, cover_letter, status, submitted_at) VALUES
('2d1c5700-2bff-4e3b-9ed5-47715e763bb9', 'a4312a99-178d-4a15-84e7-e8ed8243ae48', '8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 'Здравствуйте! Готов обсудить.', 'просмотрен', now() - interval '7 days'),
('d3e62e73-0353-407e-af84-30cf17ad3a22', 'f6c933fa-a53b-432b-be50-27f9ad400c14', 'd0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 'Здравствуйте! Готов обсудить.', 'отправлен', now() - interval '1 days'),
('1807b296-cfe5-4947-90c1-ad3e1867c59c', '72369cee-0fef-4a7b-92d1-65822ff134ce', '5d7d6ead-e106-4de2-9ef8-3536fe48f268', 'Здравствуйте! Готов обсудить.', 'принят', now() - interval '3 days'),
('8aaaa298-4d0b-4950-b6ad-5e90c10f7c6e', 'fa52fedd-5798-488e-84c1-05bad8d60f1f', 'd67cf63a-bdb1-4c30-a68d-86eb3ae28f80', 'Здравствуйте! Готов обсудить.', 'отклонен', now() - interval '1 days')
ON CONFLICT (id) DO NOTHING;

-- ============================================================================
-- Достижения ФСП (для кандидатов с привязанным fsp_id; verified=false — так
-- может быть только от самого реестра)
-- ============================================================================
INSERT INTO fsp_achievements (fsp_id, event_type, event_name, event_date, role, result_place, direction, weight, verified) VALUES
('45658ffa-7235-4de3-87c5-cfb7aed36e4a', 'обучение',     'Школа алгоритмов ФСП',                    '2024-12-15', 'participant', NULL, 'алгоритмы', 1.21, false),
('45658ffa-7235-4de3-87c5-cfb7aed36e4a', 'соревнование', 'Олимпиада «Интеллект-2025»',              '2024-07-15', 'winner',      3,    'алгоритмы', 1.47, false),
('ce6a46b9-67bc-4716-b5b9-b9a90eb4da1f', 'соревнование', 'Чемпионат по спортивному программированию','2024-07-15', 'finalist',    NULL, 'алгоритмы', 1.18, false),
('ce6a46b9-67bc-4716-b5b9-b9a90eb4da1f', 'соревнование', 'Олимпиада «Интеллект-2025»',              '2023-12-15', 'winner',      3,    'алгоритмы', 1.02, false),
('7232505f-15e1-4803-bb76-cffe4f4f588b', 'мероприятие',  'Хакатон «Цифровой прорыв»',               '2023-07-15', 'participant', NULL, 'алгоритмы', 1.59, false),
('7232505f-15e1-4803-bb76-cffe4f4f588b', 'соревнование', 'Олимпиада «Интеллект-2025»',              '2025-02-15', 'winner',      3,    'алгоритмы', 1.54, false),
('7232505f-15e1-4803-bb76-cffe4f4f588b', 'обучение',     'Школа алгоритмов ФСП',                    '2024-05-15', 'participant', NULL, 'алгоритмы', 2.28, false),
('d1aad7e1-7a28-4d70-bdb3-8685db4c200a', 'соревнование', 'Чемпионат по спортивному программированию','2025-05-15', 'finalist',    NULL, 'алгоритмы', 1.95, false),
('d1aad7e1-7a28-4d70-bdb3-8685db4c200a', 'обучение',     'Школа алгоритмов ФСП',                    '2023-04-15', 'participant', NULL, 'алгоритмы', 2.46, false),
('68b36226-ddad-4647-af83-26c47ed1133e', 'мероприятие',  'Хакатон «Цифровой прорыв»',               '2024-07-15', 'participant', NULL, 'алгоритмы', 2.33, false),
('68b36226-ddad-4647-af83-26c47ed1133e', 'соревнование', 'Чемпионат по спортивному программированию','2025-07-15', 'finalist',    NULL, 'алгоритмы', 2.37, false),
('390fff4b-aad1-4bde-bd5a-87928417eb0f', 'соревнование', 'Олимпиада «Интеллект-2025»',              '2023-12-15', 'winner',      3,    'алгоритмы', 1.62, false),
('390fff4b-aad1-4bde-bd5a-87928417eb0f', 'мероприятие',  'Хакатон «Цифровой прорыв»',               '2025-03-15', 'participant', NULL, 'алгоритмы', 1.97, false),
('390fff4b-aad1-4bde-bd5a-87928417eb0f', 'соревнование', 'Чемпионат по спортивному программированию','2024-08-15', 'finalist',    NULL, 'алгоритмы', 1.07, false)
ON CONFLICT (fsp_id, event_name, event_date) DO NOTHING;

-- ============================================================================
-- Регулярное задание от работодателя (доп. функционал) с выполненным ответом
-- ============================================================================
INSERT INTO employer_tasks (id, company_id, specialization_id, grade_id, title, description, max_score) VALUES
('02d16411-4511-4a59-aa35-2d87706b79f3', '06373a81-cd75-4172-a2f0-0c2b726d6a42', 1, 2,
 'Короткая задача: идемпотентность',
 'API платежей списывает сумму дважды при повторном запросе. Предложите подход к решению.', 10)
ON CONFLICT (id) DO NOTHING;

INSERT INTO candidate_tasks (id, task_id, candidate_id, status, answer_text, score, assigned_at, completed_at)
SELECT 'cc3b09ed-cf0f-406e-9610-d6300808b8ab', id,
       '8a3559ef-d33c-4e29-90e2-a4eddca1cc47',
       'выполнено', 'Идемпотентный ключ операции + уникальный индекс.', 8,
       now() - interval '6 days', now() - interval '5 days'
FROM employer_tasks WHERE id = '02d16411-4511-4a59-aa35-2d87706b79f3'
ON CONFLICT (task_id, candidate_id) DO NOTHING;

-- ============================================================================
-- Уведомления
-- ============================================================================
INSERT INTO notifications (user_id, type, payload) VALUES
('8a3559ef-d33c-4e29-90e2-a4eddca1cc47', 'получено_приглашение',  '{"company": "«ТехноСфера»", "status": "отправлено"}'),
('d0ab7055-db98-4ba5-a8fd-c4e8b7a69a66', 'получено_приглашение',  '{"company": "«ТехноСфера»", "status": "просмотрено"}'),
('15ca67e9-7768-4b1b-8be3-a6d567f09ba3', 'получено_приглашение',  '{"company": "«ТехноСфера»", "status": "принято"}'),
('5d7d6ead-e106-4de2-9ef8-3536fe48f268', 'получено_приглашение',  '{"company": "«ТехноСфера»", "status": "отклонено"}'),
('bb64c541-6009-459a-b0dd-6af0605c9593', 'получено_приглашение',  '{"company": "«СеверДата»",  "status": "принято"}'),
('4b5703cb-ab7b-4bda-bdc8-2a6f46836a7d', 'ответ_на_приглашение',  '{"status": "принято"}');

-- ============================================================================
-- ATS-интеграция (концепция)
-- ============================================================================
INSERT INTO ats_integrations (id, company_id, provider, config, is_active) VALUES
('a65c4a9c-119a-4774-ad21-686185acc0f2', '06373a81-cd75-4172-a2f0-0c2b726d6a42', 'custom',
 '{"endpoint":"https://ats.techsphere.ru/api","field_mapping":{"status":"stage"}}', false)
ON CONFLICT (company_id, provider) DO NOTHING;

COMMIT;

-- ============================================================================
-- Проверка результата и обновление статистики калибровки
-- ============================================================================
SELECT (SELECT count(*) FROM users WHERE email LIKE '%@demo.ru')            AS demo_users,
       (SELECT count(*) FROM test_sessions WHERE candidate_id IN (SELECT id FROM users WHERE email LIKE '%@demo.ru')) AS demo_sessions,
       (SELECT count(*) FROM candidate_categories cc JOIN users u ON u.id = cc.candidate_id WHERE u.email LIKE '%@demo.ru') AS demo_categories,
       (SELECT count(*) FROM invitations i JOIN users u ON u.id = i.candidate_id WHERE u.email LIKE '%@demo.ru') AS demo_invitations;

REFRESH MATERIALIZED VIEW v_question_stats;

-- ============================================================================
-- SEED DEMO FIX: дополнение к seed-demo.sql (выполнять ПОСЛЕ него).
--   * admin@demo.ru — администратор (активация банка вопросов в Swagger);
--   * payload уведомлений дополнен title/salary (колокольчик показывает заголовок);
--   * candidate16/17 — кандидаты «грейд не подтверждён» (для демо статуса);
-- Перезапускаемый: удаляет и пересоздаёт только свои строки (@demo.ru).
-- ============================================================================

BEGIN;

-- ---- администратор ----
DELETE FROM users WHERE email = 'admin@demo.ru';
INSERT INTO users (id, email, password_hash, role, email_confirmed, is_active, last_login_at) VALUES
('9f1c7a3e-5b2d-4e8f-9a1c-7d3e6f2b8a01', 'admin@demo.ru',
 '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2',
 'администратор', true, true, now());

-- ---- payload уведомлений: добавляем title и salary ----
UPDATE notifications SET payload = '{"company": "«ТехноСфера»", "title": "Backend-разработка", "salary": "180000–240000"}'
WHERE user_id IN (SELECT id FROM users WHERE email = 'candidate1@demo.ru') AND type = 'получено_приглашение';
UPDATE notifications SET payload = '{"company": "«ТехноСфера»", "title": "Frontend-разработка", "salary": "180000–240000"}'
WHERE user_id IN (SELECT id FROM users WHERE email = 'candidate2@demo.ru') AND type = 'получено_приглашение';
UPDATE notifications SET payload = '{"company": "«ТехноСфера»", "title": "Тестирование (QA)", "salary": "180000–240000"}'
WHERE user_id IN (SELECT id FROM users WHERE email = 'candidate3@demo.ru') AND type = 'получено_приглашение';
UPDATE notifications SET payload = '{"company": "«ТехноСфера»", "title": "Тестирование (QA)", "salary": "140000–180000"}'
WHERE user_id IN (SELECT id FROM users WHERE email = 'candidate4@demo.ru') AND type = 'получено_приглашение';
UPDATE notifications SET payload = '{"company": "«СеверДата»", "title": "Тестирование (QA)", "salary": "180000–240000"}'
WHERE user_id IN (SELECT id FROM users WHERE email = 'candidate5@demo.ru') AND type = 'получено_приглашение';
UPDATE notifications SET payload = '{"invitationId": "f764bafe-6333-4c72-8a8a-697e66f8a59c", "status": "принято", "candidate": "Иванов Пётр", "title": "Backend-разработка"}'
WHERE user_id IN (SELECT id FROM users WHERE email = 'hr@techsphere.demo.ru') AND type = 'ответ_на_приглашение';

-- ---- кандидаты «грейд не подтверждён» (демо статуса в выдаче) ----
DELETE FROM notifications WHERE user_id IN (SELECT id FROM users WHERE email IN ('candidate16@demo.ru','candidate17@demo.ru'));
DELETE FROM candidate_categories WHERE candidate_id IN (SELECT id FROM users WHERE email IN ('candidate16@demo.ru','candidate17@demo.ru'));
DELETE FROM candidate_specializations WHERE candidate_id IN (SELECT id FROM users WHERE email IN ('candidate16@demo.ru','candidate17@demo.ru'));
DELETE FROM candidate_profiles WHERE user_id IN (SELECT id FROM users WHERE email IN ('candidate16@demo.ru','candidate17@demo.ru'));
DELETE FROM users WHERE email IN ('candidate16@demo.ru','candidate17@demo.ru');

INSERT INTO users (id, email, password_hash, role, email_confirmed, is_active, last_login_at) VALUES
('7e2b1f44-9a3c-4d5e-8f1a-2b4c6d8e0f21', 'candidate16@demo.ru',
 '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат', true, true, now() - interval '1 days'),
('3c8d5e72-1f4a-4b6c-9d2e-5a7f9c1e3b54', 'candidate17@demo.ru',
 '$2b$10$AyUr7D.d3NkwBZRMFIdEqOsldJNhCfoYNSsNmpDNvyAYyx7Ij8ki2', 'кандидат', true, true, now() - interval '2 days');

INSERT INTO candidate_profiles (user_id, last_name, first_name, city, about, experience_years, soft_skills, is_profile_public, profile_completeness, last_activity_at) VALUES
('7e2b1f44-9a3c-4d5e-8f1a-2b4c6d8e0f21', 'Орлова',  'Дарья',   'Москва',          'Backend-разработчик: Go, PostgreSQL. Тест ещё не пройден.', 0.8, ARRAY['командная работа'], true, 58, now()),
('3c8d5e72-1f4a-4b6c-9d2e-5a7f9c1e3b54', 'Семёнов', 'Алексей', 'Санкт-Петербург', 'Frontend: Vue, TypeScript. Тест ещё не пройден.',           1.2, ARRAY['командная работа'], true, 61, now());

INSERT INTO candidate_skills (candidate_id, skill_id, proficiency, years_exp) VALUES
('7e2b1f44-9a3c-4d5e-8f1a-2b4c6d8e0f21', 10, 3, 1.0), ('7e2b1f44-9a3c-4d5e-8f1a-2b4c6d8e0f21', 3, 3, 0.8),
('3c8d5e72-1f4a-4b6c-9d2e-5a7f9c1e3b54', 5, 4, 1.2), ('3c8d5e72-1f4a-4b6c-9d2e-5a7f9c1e3b54', 4, 3, 1.0)
ON CONFLICT DO NOTHING;

INSERT INTO candidate_roles (candidate_id, role_id) VALUES
('7e2b1f44-9a3c-4d5e-8f1a-2b4c6d8e0f21', 1), ('3c8d5e72-1f4a-4b6c-9d2e-5a7f9c1e3b54', 1)
ON CONFLICT DO NOTHING;

-- статус «ожидает_тестирования» — в выдаче будут со статусом «грейд не подтверждён»
INSERT INTO candidate_specializations (candidate_id, specialization_id, declared_grade_id, current_grade_id, status, is_primary) VALUES
('7e2b1f44-9a3c-4d5e-8f1a-2b4c6d8e0f21', 1, 2, 2, 'ожидает_тестирования', true),
('3c8d5e72-1f4a-4b6c-9d2e-5a7f9c1e3b54', 2, 2, 2, 'ожидает_тестирования', true)
ON CONFLICT DO NOTHING;

COMMIT;

SELECT (SELECT count(*) FROM users WHERE email LIKE '%@demo.ru') AS demo_users,
       (SELECT count(*) FROM candidate_specializations cs JOIN users u ON u.id = cs.candidate_id
         WHERE u.email LIKE '%@demo.ru' AND cs.status = 'ожидает_тестирования') AS unconfirmed;
-- ============================================================================
-- ФСП / IT-вакансии: цифровая платформа-агрегатор с верифицированным профилем
-- PostgreSQL 15+
-- Стек: ASP.NET Core (C#) API + React + PostgreSQL
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;        -- полнотекстовый/неточный поиск по резюме (будущее)
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- ============================================================================
-- ЕДИНЫЙ СПРАВОЧНИК СТАТУСОВ И ТИПОВ (значения на русском — и в БД, и в API)
-- code — русское значение ('отклонено','активен',...), name — название для UI.
-- Значения добавляются INSERT'ом без миграций; выключение — через is_active.
-- code глобально уникален: близкие по смыслу значения разведены
-- ('истекло' у приглашения / 'срок_истек' у задания).
-- ============================================================================
CREATE TABLE statuses (
    id         SMALLSERIAL PRIMARY KEY,
    code       TEXT NOT NULL UNIQUE,        -- 'отклонено','активен','завершен',...
    entity     TEXT NOT NULL,               -- где используется: 'роль','статус_приглашения',...
    name       TEXT NOT NULL,               -- русское название для UI
    sort_order SMALLINT NOT NULL DEFAULT 0, -- порядок в выпадающих списках
    is_active  BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE INDEX idx_statuses_entity ON statuses(entity, sort_order);
-- btree_gist — для периодов и непересекающихся диапазонов (зарплата/бронирования, задел на будущее)

-- ============================================================================
-- 1. ПОЛЬЗОВАТЕЛИ И АВТОРИЗАЦИЯ
-- ============================================================================
-- users.id = UUID — совместимость с Keycloak (ФСП ID в будущем),
-- при миграции на Keycloak внешний идентификатор сольётся с этим PK.
CREATE TABLE users (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email               CITEXT NOT NULL UNIQUE,          -- регистрация по email с подтверждением
    password_hash       TEXT,                            -- nullable: при переходе на Keycloak пароль уходит в IdP
    role                TEXT NOT NULL REFERENCES statuses(code),  -- entity: роль
    email_confirmed     BOOLEAN NOT NULL DEFAULT FALSE,
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    -- Связь с ФСП: участник ФСП может отсутствовать — обязательна корректная
    -- обработка NULL (ТЗ п.5 критериев оценки, п.2 описания задачи).
    fsp_id              UUID UNIQUE,                     -- ID участника ФСП (будущий ФСП ID / Keycloak)
    fsp_linked_at       TIMESTAMPTZ,                     -- когда привязали профиль к ФСП
    last_login_at       TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_users_role        ON users(role) WHERE is_active;
CREATE INDEX idx_users_fsp_id      ON users(fsp_id) WHERE fsp_id IS NOT NULL;

-- Одноразовые токены подтверждения email / сброса пароля
CREATE TABLE auth_tokens (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_type   TEXT NOT NULL REFERENCES statuses(code),  -- entity: тип_токена
    token_hash   TEXT NOT NULL UNIQUE,                   -- храним только хэш токена
    expires_at   TIMESTAMPTZ NOT NULL,
    used_at      TIMESTAMPTZ,                            -- NULL = токен активен
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_auth_tokens_user ON auth_tokens(user_id, token_type, expires_at);

-- Журнал согласий пользователя (152-ФЗ, ТЗ п.3.5 «Безопасность»)
CREATE TABLE consent_log (
    id           BIGSERIAL PRIMARY KEY,
    user_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    consent_type TEXT NOT NULL REFERENCES statuses(code),  -- entity: тип_согласия
    granted      BOOLEAN NOT NULL,
    terms_version TEXT NOT NULL,                         -- версия оферты/политики
    granted_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    ip_address   INET,
    revoked_at   TIMESTAMPTZ                             -- отзыв согласия
);
CREATE INDEX idx_consent_log_user ON consent_log(user_id, consent_type, granted_at DESC);

-- ============================================================================
-- 2. СПРАВОЧНИКИ (команда ведёт сама, опираясь на общепринятые в IT понятия)
-- ============================================================================
-- Отрасль/направление: Backend, Frontend, DevOps, QA, Data, Mobile...
CREATE TABLE specializations (
    id          SMALLSERIAL PRIMARY KEY,
    code        TEXT NOT NULL UNIQUE,                    -- 'Backend','Frontend','тестирование',...
    name        TEXT NOT NULL,                           -- 'Backend-разработка'
    description TEXT,
    parent_id   SMALLINT REFERENCES specializations(id), -- иерархия: backend -> backend/java
    sort_order  SMALLINT NOT NULL DEFAULT 0,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE
);

-- Грейды: строгая линейка для смены грейда по результатам теста
CREATE TABLE grades (
    id             SMALLSERIAL PRIMARY KEY,
    code           TEXT NOT NULL UNIQUE CHECK (code IN ('стажер','джуниор','миддл','сеньор','лид','архитектор')),
    name           TEXT NOT NULL,                        -- 'Стажёр','Джуниор','Миддл',...
    rank           SMALLINT NOT NULL UNIQUE,             -- 1..6 — для сравнения «выше/ниже»
    min_exp_years  NUMERIC(4,1),                         -- рекомендуемый стаж, справочно
    description    TEXT,
    is_active      BOOLEAN NOT NULL DEFAULT TRUE
);

-- Роли в команде (Team Lead, Разработчик, Тимлид, ...)
CREATE TABLE roles (
    id        SMALLSERIAL PRIMARY KEY,
    name      TEXT NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Категории скиллов (справочник, чтобы не дублировать строки в skills)
CREATE TABLE skill_categories (
    id   SMALLSERIAL PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,                           -- 'Язык','Фреймворк','БД','Инфраструктура','Инструменты'
    sort_order SMALLINT NOT NULL DEFAULT 0
);

-- Скиллы/технологии (стек)
CREATE TABLE skills (
    id          SMALLSERIAL PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,                    -- 'C#','React','PostgreSQL'
    category_id SMALLINT NOT NULL REFERENCES skill_categories(id),
    is_active   BOOLEAN NOT NULL DEFAULT TRUE
);

-- КАТЕГОРИЯ = специализация + грейд (ТЗ: «категория — группа однотипных соискателей»)
CREATE TABLE categories (
    id               SMALLSERIAL PRIMARY KEY,
    specialization_id SMALLINT NOT NULL REFERENCES specializations(id),
    grade_id         SMALLINT NOT NULL REFERENCES grades(id),
    display_name     TEXT NOT NULL,                      -- 'Backend-разработка (Миддл)'
    is_active        BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (specialization_id, grade_id)
);

-- ============================================================================
-- 3. ПРОФИЛИ: КАНДИДАТ И РАБОТОДАТЕЛЬ
-- ============================================================================
CREATE TABLE candidate_profiles (
    user_id            UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    last_name          TEXT NOT NULL,
    first_name         TEXT NOT NULL,
    middle_name        TEXT,
    -- Контакты: скрыты от работодателя до принятия приглашения/отклика (ТЗ 2.2 п.2)
    phone              TEXT,
    telegram           TEXT,
    city               TEXT,
    about              TEXT,                             -- краткое описание / самопрезентация
    experience_years   NUMERIC(4,1) NOT NULL DEFAULT 0,  -- стаж
    soft_skills        TEXT[],                           -- софт-скиллы
    resume_pdf_path    TEXT,                             -- исходный загруженный PDF
    profile_pdf_path   TEXT,                             -- автогенерированный стандартный PDF-профиль
    is_profile_public  BOOLEAN NOT NULL DEFAULT TRUE,    -- приватность (настройки кабинета)
    show_contacts      BOOLEAN NOT NULL DEFAULT FALSE,   -- доп. приватность контактов
    profile_completeness SMALLINT NOT NULL DEFAULT 0,    -- % заполненности, для ранжирования
    last_activity_at   TIMESTAMPTZ,                      -- активность профиля (регулярные задания)
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);
-- поиск по ФИО/о себе
CREATE INDEX idx_candidate_profiles_trgm ON candidate_profiles USING gin (
    (coalesce(last_name,'') || ' ' || coalesce(first_name,'') || ' ' || coalesce(about,'')) gin_trgm_ops);

-- Стек кандидата с уровнем владения (для фильтров и подборки)
CREATE TABLE candidate_skills (
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    skill_id     SMALLINT NOT NULL REFERENCES skills(id),
    proficiency  SMALLINT NOT NULL DEFAULT 1
                 CHECK (proficiency BETWEEN 1 AND 5),
    years_exp    NUMERIC(4,1),
    PRIMARY KEY (candidate_id, skill_id)
);

-- Роли кандидата (может совмещать)
CREATE TABLE candidate_roles (
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    role_id      SMALLINT NOT NULL REFERENCES roles(id),
    PRIMARY KEY (candidate_id, role_id)
);

-- Компания работодателя (у пользователя-работодателя может быть несколько компаний в будущем)
CREATE TABLE companies (
    id             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    owner_user_id  UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name           TEXT NOT NULL,
    industry       TEXT,                                 -- направление деятельности
    description    TEXT,
    website        TEXT,
    contact_email  CITEXT,
    contact_phone  TEXT,
    logo_path      TEXT,
    status         TEXT NOT NULL DEFAULT 'активен' REFERENCES statuses(code),  -- entity: статус_сущности
    -- Будущее: верификация компании (защита от недобросовестных работодателей, вне MVP)
    verified_at    TIMESTAMPTZ,
    verification_level SMALLINT NOT NULL DEFAULT 0,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_companies_owner ON companies(owner_user_id);

-- ============================================================================
-- 4. ОНБОРДИНГ: ОПРОС, ЗАЯВЛЕННЫЙ ГРЕЙД, КАТЕГОРИЯ КАНДИДАТА
-- ============================================================================
-- Сессия опроса по отрасли и специализации (история опроса — требование ТЗ)
CREATE TABLE survey_sessions (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    started_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    source       TEXT NOT NULL DEFAULT 'регистрация'     -- регистрация / обновление_профиля
);
CREATE INDEX idx_survey_sessions_candidate ON survey_sessions(candidate_id, started_at DESC);

-- Ответы опроса (свободная схема — команда сама решает состав вопросов)
CREATE TABLE survey_answers (
    id          BIGSERIAL PRIMARY KEY,
    session_id  UUID NOT NULL REFERENCES survey_sessions(id) ON DELETE CASCADE,
    question_key TEXT NOT NULL,                          -- 'отрасль','специализация','формат_работы',...
    answer_value JSONB NOT NULL,                         -- { "value": "бэкенд" } / { "values": [...] }
    answered_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_survey_answers_session ON survey_answers(session_id, question_key);

-- Заявленный/подтверждённый грейд кандидата по каждой специализации.
-- current_grade_id — то, что видит работодатель; определяется только тестом.
CREATE TABLE candidate_specializations (
    candidate_id        UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    specialization_id   SMALLINT NOT NULL REFERENCES specializations(id),
    declared_grade_id   SMALLINT NOT NULL REFERENCES grades(id),    -- самостоятельно выбранный при регистрации
    current_grade_id    SMALLINT NOT NULL REFERENCES grades(id),    -- фактический после теста (может отличаться)
    status              TEXT NOT NULL DEFAULT 'ожидает_тестирования'
                        REFERENCES statuses(code),  -- entity: статус_специализации
    last_test_at        TIMESTAMPTZ,
    last_grade_change_at TIMESTAMPTZ,                    -- ограничение частоты смены грейда (например, раз в 3 мес.)
    is_primary          BOOLEAN NOT NULL DEFAULT FALSE,  -- основная специализация (для выдачи)
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (candidate_id, specialization_id)
);
-- Текущая категория кандидата (денормализация для быстрой выдачи по категориям)
CREATE TABLE candidate_categories (
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    category_id  SMALLINT NOT NULL REFERENCES categories(id),
    is_primary   BOOLEAN NOT NULL DEFAULT FALSE,
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (candidate_id, category_id)
);
CREATE INDEX idx_candidate_categories_cat ON candidate_categories(category_id);

-- ============================================================================
-- 5. ТЕСТИРОВАНИЕ (определяет грейд и ранжирование внутри категории)
-- ============================================================================
-- Шаблон теста под пару «специализация + грейд»
CREATE TABLE test_assignments (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    specialization_id SMALLINT NOT NULL REFERENCES specializations(id),
    grade_id         SMALLINT NOT NULL REFERENCES grades(id),
    title            TEXT NOT NULL,
    description      TEXT,
    duration_minutes SMALLINT NOT NULL DEFAULT 30,
    pass_threshold   NUMERIC(5,2) NOT NULL DEFAULT 60.00, -- процент для подтверждения грейда
    questions_count  SMALLINT NOT NULL DEFAULT 10,
    -- Стратифицированная выборка: сколько вопросов какой сложности тянуть из банка.
    -- Итог сопоставим по сложности между разными кандидатами, наборы при этом уникальны.
    difficulty_mix   JSONB NOT NULL DEFAULT '{"1":3,"2":4,"3":2,"4":1}'::jsonb,
    version          INTEGER NOT NULL DEFAULT 1,          -- версионирование банка вопросов
    is_active        BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (specialization_id, grade_id, version)
);

-- Банк вопросов. Против распространения заданий (ТЗ): вопросов много,
-- в сессию случайно отбирается подмножество, у каждого вопроса есть набор
-- эквивалентных вариантов (question_group) и периодическая ротация.
CREATE TABLE test_questions (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    specialization_id SMALLINT NOT NULL REFERENCES specializations(id),
    grade_id         SMALLINT NOT NULL REFERENCES grades(id),
    question_type    TEXT NOT NULL DEFAULT 'тест_с_вариантами'  -- тест_с_вариантами | задача_с_кодом | открытый_вопрос
                     REFERENCES statuses(code),  -- entity: тип_вопроса
    question_group   UUID NOT NULL DEFAULT uuid_generate_v4(), -- варианты одной задачи
    difficulty       SMALLINT NOT NULL DEFAULT 1 CHECK (difficulty BETWEEN 1 AND 5),
    prompt           TEXT NOT NULL,                      -- формулировка (для code — условие + тесты)
    payload          JSONB NOT NULL DEFAULT '{}'::jsonb, -- варианты ответов, тест-кейсы для кода, рубрика для open
    correct_payload  JSONB NOT NULL DEFAULT '{}'::jsonb, -- эталон: верный вариант / эталонное решение / критерии
    max_score        NUMERIC(6,2) NOT NULL DEFAULT 1,
    is_active        BOOLEAN NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_test_questions_lookup ON test_questions(specialization_id, grade_id, question_type, is_active);
CREATE INDEX idx_test_questions_group  ON test_questions(question_group);

-- Прохождение теста кандидатом
CREATE TABLE test_sessions (
    id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id      UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    specialization_id SMALLINT NOT NULL REFERENCES specializations(id),
    target_grade_id   SMALLINT NOT NULL REFERENCES grades(id),   -- на какой грейд претендует
    assignment_id     UUID REFERENCES test_assignments(id),
    status            TEXT NOT NULL DEFAULT 'в_процессе'
                      REFERENCES statuses(code),  -- entity: статус_теста
    attempt_no        SMALLINT NOT NULL DEFAULT 1,
    started_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    finished_at       TIMESTAMPTZ,
    total_score       NUMERIC(8,2),
    max_score         NUMERIC(8,2),
    percent           NUMERIC(5,2) GENERATED ALWAYS AS
                      (CASE WHEN max_score > 0 THEN round(total_score / max_score * 100, 2) END) STORED,
    passed            BOOLEAN,                           -- NULL пока не проверен open-тип
    resulting_grade_id SMALLINT REFERENCES grades(id),   -- присвоенный грейд по итогу
    -- Антифрод: защита от утечек заданий встроена в механику банка
    -- (изоморфные группы + рандомная выборка по difficulty_mix).
    -- Детект списывания — запросом по test_session_answers (одинаковые
    -- ответы разных кандидатов на один вопрос), доп. полей не требуется.
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_test_sessions_candidate ON test_sessions(candidate_id, specialization_id, started_at DESC);
CREATE INDEX idx_test_sessions_result    ON test_sessions(specialization_id, resulting_grade_id, passed)
    WHERE status = 'завершен';

-- Вопросы конкретной сессии + ответы (рандомизированный набор фиксируется)
CREATE TABLE test_session_answers (
    id          BIGSERIAL PRIMARY KEY,
    session_id  UUID NOT NULL REFERENCES test_sessions(id) ON DELETE CASCADE,
    question_id UUID NOT NULL REFERENCES test_questions(id),
    order_no    SMALLINT NOT NULL,
    answer_payload JSONB,                                -- ответ кандидата
    score       NUMERIC(6,2),
    auto_graded BOOLEAN NOT NULL DEFAULT TRUE,           -- open-вопросы — ручная/экспертная проверка
    graded_at   TIMESTAMPTZ,
    answered_at TIMESTAMPTZ,
    UNIQUE (session_id, question_id)
);

-- История смен грейдов (аудит + ограничение частоты смены: не чаще 1 раза в N дней)
CREATE TABLE grade_change_history (
    id           BIGSERIAL PRIMARY KEY,
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    specialization_id SMALLINT NOT NULL REFERENCES specializations(id),
    old_grade_id SMALLINT REFERENCES grades(id),
    new_grade_id SMALLINT NOT NULL REFERENCES grades(id),
    reason       TEXT NOT NULL REFERENCES statuses(code),  -- entity: причина_смены_грейда
    test_session_id UUID REFERENCES test_sessions(id),
    changed_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_grade_history_candidate ON grade_change_history(candidate_id, specialization_id, changed_at DESC);

-- ============================================================================
-- 6. ВАКАНСИИ И ОТКЛИКИ (опционально для MVP, но заложено сразу)
-- ============================================================================
CREATE TABLE vacancies (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    company_id       UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    title            TEXT NOT NULL,
    description      TEXT NOT NULL,                      -- «чем занимается команда»
    specialization_id SMALLINT NOT NULL REFERENCES specializations(id),
    grade_from_id    SMALLINT NOT NULL REFERENCES grades(id),  -- грейдовый диапазон
    grade_to_id      SMALLINT NOT NULL REFERENCES grades(id),
    salary_from      INTEGER NOT NULL CHECK (salary_from > 0),   -- ТЗ 2.2 п.1: ЗП обязательна
    salary_to        INTEGER NOT NULL CHECK (salary_to >= salary_from),
    currency         CHAR(3) NOT NULL DEFAULT 'RUB',
    employment_type  TEXT,                               -- fulltime/parttime/remote
    location         TEXT,
    status           TEXT NOT NULL DEFAULT 'черновик' REFERENCES statuses(code),  -- entity: статус_сущности
    published_at     TIMESTAMPTZ,
    expires_at       TIMESTAMPTZ,
    views_count      INTEGER NOT NULL DEFAULT 0,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_vacancies_search ON vacancies(specialization_id, status, published_at DESC)
    WHERE status = 'активен';
CREATE INDEX idx_vacancies_salary ON vacancies(salary_from, salary_to);

CREATE TABLE vacancy_skills (
    vacancy_id UUID NOT NULL REFERENCES vacancies(id) ON DELETE CASCADE,
    skill_id   SMALLINT NOT NULL REFERENCES skills(id),
    PRIMARY KEY (vacancy_id, skill_id)
);

-- Отклик кандидата на вакансию (классический сценарий — дополнительный к механике приглашений)
CREATE TABLE applications (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    vacancy_id   UUID NOT NULL REFERENCES vacancies(id) ON DELETE CASCADE,
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    cover_letter TEXT,
    status       TEXT NOT NULL DEFAULT 'отправлен' REFERENCES statuses(code),  -- entity: статус_отклика
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (vacancy_id, candidate_id)
);
CREATE INDEX idx_applications_candidate ON applications(candidate_id, status, submitted_at DESC);
CREATE INDEX idx_applications_vacancy   ON applications(vacancy_id, status);

-- ============================================================================
-- 7. ПРИГЛАШЕНИЯ (основная механика выхода на контакт: инициатива работодателя)
-- ============================================================================
-- Без обязательной привязки к вакансии (ТЗ 2.1 «Работодатель»: приглашение может
-- быть направлено напрямую, без опубликованной вакансии).
CREATE TABLE invitations (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    company_id   UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    vacancy_id   UUID REFERENCES vacancies(id) ON DELETE SET NULL, -- nullable — прямое предложение
    title        TEXT NOT NULL,
    message      TEXT NOT NULL,                          -- описание предложения
    salary_from  INTEGER NOT NULL CHECK (salary_from > 0),   -- ЗП обязательна (ТЗ 2.2 п.1)
    salary_to    INTEGER NOT NULL CHECK (salary_to >= salary_from),
    currency     CHAR(3) NOT NULL DEFAULT 'RUB',
    contact_method TEXT NOT NULL DEFAULT 'почта'         -- как связаться после принятия
                 REFERENCES statuses(code),  -- entity: способ_связи
    status       TEXT NOT NULL DEFAULT 'отправлено' REFERENCES statuses(code),  -- entity: статус_приглашения
    sent_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    viewed_at    TIMESTAMPTZ,
    responded_at TIMESTAMPTZ,
    expires_at   TIMESTAMPTZ,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_invitations_candidate ON invitations(candidate_id, status, sent_at DESC);
CREATE INDEX idx_invitations_company   ON invitations(company_id, status, sent_at DESC);

-- ============================================================================
-- 8. ПОИСК И ПОДБОРКА РАБОТОДАТЕЛЯ (фильтрация без потери ранее полученной подборки)
-- ============================================================================
-- Сохранённый поисковый запрос работодателя
CREATE TABLE employer_search_requests (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    company_id  UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    title       TEXT,
    filters     JSONB NOT NULL,      -- {specialization_id, grade_id, skills[], min_score, has_fsp, city,...}
    created_by  UUID NOT NULL REFERENCES users(id),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_search_requests_company ON employer_search_requests(company_id, created_at DESC);

-- Результат подборки: кандидаты с рангом и объяснимостью выдачи (ТЗ критерий 1)
CREATE TABLE employer_search_results (
    request_id   UUID NOT NULL REFERENCES employer_search_requests(id) ON DELETE CASCADE,
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    rank_score   NUMERIC(8,3) NOT NULL,                  -- итоговый скор ранжирования
    explanation  JSONB NOT NULL DEFAULT '{}'::jsonb,     -- «почему в выдаче»: совпадения, скор теста, достижения ФСП
    position     SMALLINT NOT NULL,                      -- позиция в топе
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (request_id, candidate_id)
);
CREATE INDEX idx_search_results_rank ON employer_search_results(request_id, rank_score DESC);

-- ============================================================================
-- 9. РЕГУЛЯРНЫЕ КОРОТКИЕ ЗАДАНИЯ (необязательно для MVP, заложено)
-- ============================================================================
CREATE TABLE employer_tasks (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    company_id       UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    specialization_id SMALLINT NOT NULL REFERENCES specializations(id),
    grade_id         SMALLINT REFERENCES grades(id),
    title            TEXT NOT NULL,
    description      TEXT NOT NULL,
    payload          JSONB NOT NULL DEFAULT '{}'::jsonb, -- задача/кейс от работодателя
    max_score        NUMERIC(6,2),
    is_active        BOOLEAN NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Назначение задания кандидату (система периодически предлагает короткие задачи)
CREATE TABLE candidate_tasks (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    task_id      UUID NOT NULL REFERENCES employer_tasks(id) ON DELETE CASCADE,
    candidate_id UUID NOT NULL REFERENCES candidate_profiles(user_id) ON DELETE CASCADE,
    status         TEXT NOT NULL DEFAULT 'назначено' REFERENCES statuses(code),  -- entity: статус_задания
    answer_text  TEXT,                                   -- решение или подход к решению
    answer_payload JSONB,
    score        NUMERIC(6,2),
    assigned_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ,
    UNIQUE (task_id, candidate_id)
);
CREATE INDEX idx_candidate_tasks_candidate ON candidate_tasks(candidate_id, status, assigned_at DESC);

-- ============================================================================
-- 10. ДОСТИЖЕНИЯ ФСП (структуру команда определяет сама; влияют на ранжирование)
-- ============================================================================
-- Привязка через fsp_id = users.fsp_id. Команда сама решает, какие сведения
-- хранить и показывать; обязательна корректная обработка кандидата без истории.
CREATE TABLE fsp_achievements (
    id             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    fsp_id         UUID NOT NULL REFERENCES users(fsp_id) ON DELETE CASCADE, -- участник ФСП
    event_type     TEXT NOT NULL REFERENCES statuses(code),  -- entity: тип_события_фсп
    event_name     TEXT NOT NULL,                        -- название соревнования/мероприятия
    event_date     DATE NOT NULL,
    role           TEXT,                                 -- 'participant','winner','finalist','jury'
    result_place   SMALLINT,                             -- место, если есть
    result_score   NUMERIC(8,2),                         -- балл рейтинга ФСП, если доступен
    direction      TEXT,                                 -- направление (алгоритмы, веб, ...)
    source_payload JSONB NOT NULL DEFAULT '{}'::jsonb,   -- сырые данные из будущей интеграции с реестром
    weight         NUMERIC(4,2) NOT NULL DEFAULT 1.00,   -- вес достижения для ранжирования
    verified       BOOLEAN NOT NULL DEFAULT FALSE,       -- подтверждено ФСП (при будущей интеграции)
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (fsp_id, event_name, event_date)              -- защита от дублей при повторной загрузке
);
CREATE INDEX idx_fsp_achievements_fsp ON fsp_achievements(fsp_id, event_date DESC);

-- ============================================================================
-- 11. УВЕДОМЛЕНИЯ, АУДИТ, БУДУЩАЯ ИНТЕГРАЦИЯ (ATS / верификация / антифрод)
-- ============================================================================
CREATE TABLE notifications (
    id        UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id   UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type      TEXT NOT NULL,                             -- 'получено_приглашение','смена_статуса_отклика',...
    payload   JSONB NOT NULL DEFAULT '{}'::jsonb,
    read_at   TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_notifications_user ON notifications(user_id, read_at, created_at DESC);

-- Аудит действий (152-ФЗ, разграничение прав, восстановление истории)
CREATE TABLE audit_log (
    id         BIGSERIAL PRIMARY KEY,
    user_id    UUID REFERENCES users(id) ON DELETE SET NULL,
    entity     TEXT NOT NULL,                            -- 'invitations','candidate_profiles',...
    entity_id  TEXT NOT NULL,
    action     TEXT NOT NULL,                            -- 'insert','update','delete','view_contacts'
    old_value  JSONB,
    new_value  JSONB,
    ip_address INET,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_audit_entity ON audit_log(entity, entity_id, created_at DESC);

-- Будущее: интеграция с ATS работодателя (вне MVP, ТЗ «Будет плюсом»)
CREATE TABLE ats_integrations (
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    provider   TEXT NOT NULL,                            -- 'greenhouse','lever','custom',...
    config     JSONB NOT NULL DEFAULT '{}'::jsonb,       -- endpoint, credentials_ref, field_mapping
    is_active  BOOLEAN NOT NULL DEFAULT FALSE,
    last_sync_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (company_id, provider)
);

-- Будущее: жалобы на фиктивные вакансии / недобросовестных работодателей
CREATE TABLE vacancy_reports (
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    vacancy_id UUID REFERENCES vacancies(id) ON DELETE CASCADE,
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    reporter_id UUID REFERENCES users(id) ON DELETE SET NULL,
    reason     TEXT NOT NULL,
    details    TEXT,
    status     TEXT NOT NULL DEFAULT 'активен' REFERENCES statuses(code),  -- entity: статус_сущности; активен=на рассмотрении, заблокирован=решено
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ============================================================================
-- ФУНКЦИИ / ТРИГГЕРЫ
-- ============================================================================
-- Автообновление updated_at
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
DECLARE t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'users','candidate_profiles','companies','candidate_specializations',
        'vacancies','applications'
    ] LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS trg_updated_at ON %I', t);
        EXECUTE format('CREATE TRIGGER trg_updated_at BEFORE UPDATE ON %I
                        FOR EACH ROW EXECUTE FUNCTION set_updated_at()', t);
    END LOOP;
END $$;

-- Ограничение частоты смены грейда (например, не чаще 1 раза в 90 дней).
-- Проверка выполняется по grade_change_history.
CREATE OR REPLACE FUNCTION check_grade_change_cooldown(p_candidate UUID, p_spec SMALLINT, p_cooldown_days INT DEFAULT 90)
RETURNS BOOLEAN AS $$
DECLARE last_change TIMESTAMPTZ;
BEGIN
    SELECT changed_at INTO last_change
    FROM grade_change_history
    WHERE candidate_id = p_candidate AND specialization_id = p_spec
    ORDER BY changed_at DESC LIMIT 1;
    RETURN last_change IS NULL OR last_change <= now() - make_interval(days => p_cooldown_days);
END;
$$ LANGUAGE plpgsql STABLE;

-- ============================================================================
-- ПРЕДСТАВЛЕНИЯ (Views)
-- ============================================================================
-- Ранжирование кандидатов внутри категории: лучше результаты теста выше,
-- подтверждённые достижения ФСП повышают позицию (ТЗ критерий 1 и п.2).
CREATE OR REPLACE VIEW v_category_rankings AS
SELECT
    cc.category_id,
    cs.candidate_id,
    MAX(ts.percent)                                   AS best_test_percent,
    BOOL_OR(ts.passed)                                AS best_test_passed,
    COALESCE(SUM(fa.weight * CASE WHEN fa.verified THEN 1.5 ELSE 1 END), 0)
                                                      AS fsp_achievements_weight,
    (COALESCE(MAX(ts.percent),0) * 0.7
     + LEAST(COALESCE(SUM(fa.weight),0), 30) * 1.0)    AS rank_score,
    ROW_NUMBER() OVER (
        PARTITION BY cc.category_id
        ORDER BY (COALESCE(MAX(ts.percent),0) * 0.7
                  + LEAST(COALESCE(SUM(fa.weight),0), 30)) DESC,
                 COALESCE(MAX(ts.percent),0) DESC
    )                                                 AS position_in_category
FROM candidate_categories cc
JOIN candidate_specializations cs
  ON cs.candidate_id = cc.candidate_id
JOIN categories c ON c.id = cc.category_id
 AND cs.specialization_id = c.specialization_id
 AND cs.current_grade_id  = c.grade_id
LEFT JOIN test_sessions ts
  ON ts.candidate_id = cs.candidate_id
 AND ts.specialization_id = cs.specialization_id
 AND ts.status = 'завершен' AND ts.passed = TRUE
LEFT JOIN users u ON u.id = cs.candidate_id
LEFT JOIN fsp_achievements fa ON fa.fsp_id = u.fsp_id
GROUP BY cc.category_id, cs.candidate_id;

-- Карточка кандидата, видимая работодателю ДО принятия приглашения/отклика:
-- без контактов (ТЗ 2.2 п.2). Контакты отдаются отдельным эндпоинтом после accept.
CREATE OR REPLACE VIEW v_candidate_public_card AS
SELECT
    cp.user_id                                        AS candidate_id,
    cp.last_name, cp.first_name, cp.middle_name,
    cp.city, cp.about, cp.experience_years, cp.soft_skills,
    cp.profile_pdf_path,
    u.fsp_id,
    (u.fsp_id IS NOT NULL)                            AS has_fsp_history,
    cs.specialization_id, cs.current_grade_id,
    cp.is_profile_public, cp.last_activity_at
FROM candidate_profiles cp
JOIN users u ON u.id = cp.user_id
LEFT JOIN candidate_specializations cs
  ON cs.candidate_id = cp.user_id AND cs.is_primary = TRUE;

COMMENT ON VIEW v_candidate_public_card IS
'Контакты (phone, telegram, email) намеренно исключены — доступны только после принятия приглашения/отклика';


-- Статистика вопросов банка для калибровки (дискриминативность, сложность).
-- Источник для решений: какие вопросы ротировать/переписывать.
CREATE MATERIALIZED VIEW IF NOT EXISTS v_question_stats AS
SELECT
    q.id          AS question_id,
    q.specialization_id,
    q.grade_id,
    q.difficulty,
    COUNT(a.id)                                            AS attempts,
    AVG(a.score / NULLIF(q.max_score, 0))                  AS avg_relative_score,
    -- дискриминативность: корреляция балла за вопрос с процентом всей сессии
    CORR(a.score / NULLIF(q.max_score, 0), ts.percent)     AS discrimination_index
FROM test_questions q
JOIN test_session_answers a ON a.question_id = q.id
JOIN test_sessions ts       ON ts.id = a.session_id AND ts.status = 'завершен'
GROUP BY q.id;
CREATE UNIQUE INDEX IF NOT EXISTS idx_v_question_stats ON v_question_stats(question_id);

-- ============================================================================
-- НАПОЛНЕНИЕ СПРАВОЧНИКОВ (базовый набор, MVP)
-- ============================================================================
INSERT INTO statuses (code, entity, name, sort_order) VALUES
 -- роли пользователей
 ('кандидат','роль','Кандидат',1),('работодатель','роль','Работодатель',2),('администратор','роль','Администратор',3),
 -- токены авторизации
 ('подтверждение_почты','тип_токена','Подтверждение email',1),('сброс_пароля','тип_токена','Сброс пароля',2),('смена_почты','тип_токена','Смена email',3),
 -- согласия (152-ФЗ)
 ('обработка_персональных_данных','тип_согласия','Обработка персональных данных',1),
 ('публикация_профиля','тип_согласия','Публикация профиля',2),
 ('обработка_данных_фсп','тип_согласия','Обработка данных ФСП',3),
 ('условия_сервиса','тип_согласия','Условия использования сервиса',4),
 -- общий статус сущности
 ('черновик','статус_сущности','Черновик',1),('активен','статус_сущности','Активен',2),
 ('в_архиве','статус_сущности','В архиве',3),('заблокирован','статус_сущности','Заблокирован',4),
 -- типы вопросов теста
 ('тест_с_вариантами','тип_вопроса','Тест с вариантами ответа',1),('задача_с_кодом','тип_вопроса','Задача с кодом',2),('открытый_вопрос','тип_вопроса','Открытый вопрос',3),
 -- статус специализации кандидата
 ('ожидает_тестирования','статус_специализации','Ожидает тестирования',1),('грейд_подтвержден','статус_специализации','Грейд подтверждён',2),
 ('грейд_понижен','статус_специализации','Грейд понижен',3),('грейд_повышен','статус_специализации','Грейд повышен',4),
 -- статус тестовой сессии
 ('в_процессе','статус_теста','В процессе',1),('завершен','статус_теста','Завершён',2),
 ('прерван','статус_теста','Прерван',3),('истек_срок','статус_теста','Истёк срок',4),
 -- причины смены грейда
 ('первичное_тестирование','причина_смены_грейда','Первичное тестирование',1),
 ('пересдача_повышение','причина_смены_грейда','Пересдача: повышение',2),
 ('пересдача_понижение','причина_смены_грейда','Пересдача: понижение',3),
 ('изменено_вручную','причина_смены_грейда','Изменено вручную',4),
 -- статус отклика на вакансию
 ('отправлен','статус_отклика','Отправлен',1),('просмотрен','статус_отклика','Просмотрен',2),
 ('принят','статус_отклика','Принят',3),('отклонен','статус_отклика','Отклонён',4),('отозван','статус_отклика','Отозван',5),
 -- статус приглашения
 ('отправлено','статус_приглашения','Отправлено',1),('просмотрено','статус_приглашения','Просмотрено',2),('принято','статус_приглашения','Принято',3),
 ('отклонено','статус_приглашения','Отклонено',4),('истекло','статус_приглашения','Истекло',5),('отозвано','статус_приглашения','Отозвано',6),
 -- способ связи
 ('почта','способ_связи','Email',1),('телефон','способ_связи','Телефон',2),
 ('телеграм','способ_связи','Telegram',3),('через_приложение','способ_связи','Через приложение',4),
 -- статус назначенного задания
 ('назначено','статус_задания','Назначено',1),('в_работе','статус_задания','В работе',2),
 ('выполнено','статус_задания','Выполнено',3),('пропущено','статус_задания','Пропущено',4),('срок_истек','статус_задания','Истёк срок',5),
 -- тип события ФСП
 ('соревнование','тип_события_фсп','Соревнование',1),('мероприятие','тип_события_фсп','Мероприятие',2),
 ('обучение','тип_события_фсп','Обучение',3),('прочее','тип_события_фсп','Прочее',4)
ON CONFLICT (code) DO NOTHING;

INSERT INTO grades (code, name, rank, min_exp_years) VALUES
 ('стажер','Стажёр',1,0),('джуниор','Джуниор',2,0.5),('миддл','Миддл',3,2),
 ('сеньор','Сеньор',4,5),('лид','Лид',5,8),('архитектор','Архитектор',6,10)
ON CONFLICT (code) DO NOTHING;

INSERT INTO specializations (code, name, sort_order) VALUES
 ('Backend','Backend-разработка',1),('Frontend','Frontend-разработка',2),
 ('мобильная_разработка','Мобильная разработка',3),('DevOps','DevOps / SRE',4),
 ('тестирование','Тестирование (QA)',5),('Data_Science','Data Science / ML',6),
 ('аналитика','Системная аналитика',7),('информационная_безопасность','Информационная безопасность',8),
 ('Gamedev','Gamedev',9),('Embedded','Embedded / IoT',10)
ON CONFLICT (code) DO NOTHING;

INSERT INTO skill_categories (name, sort_order) VALUES
 ('Язык',1),('Фреймворк',2),('БД',3),('Инфраструктура',4),('Инструменты',5)
ON CONFLICT (name) DO NOTHING;

INSERT INTO skills (name, category_id) VALUES
 ('C#',1),('.NET',2),('ASP.NET Core',2),
 ('JavaScript',1),('TypeScript',1),('React',2),
 ('Node.js',2),('Python',1),('SQL',1),
 ('PostgreSQL',3),('Docker',4),('Kubernetes',4),
 ('Git',5),('Linux',4)
ON CONFLICT (name) DO NOTHING;

-- Категории = декартово произведение специализаций и грейдов (можно ограничить)
INSERT INTO categories (specialization_id, grade_id, display_name)
SELECT s.id, g.id, s.name || ' (' || g.name || ')'
FROM specializations s CROSS JOIN grades g
ON CONFLICT (specialization_id, grade_id) DO NOTHING;

-- Роли
INSERT INTO roles (name) VALUES
 ('Разработчик'),('Team Lead'),('Tech Lead'),('Архитектор'),
 ('QA-инженер'),('Аналитик'),('DevOps-инженер')
ON CONFLICT (name) DO NOTHING;


-- Таблица участников ФСП: fsp_id — настоящий PK
CREATE TABLE fsp_profiles (
    fsp_id      UUID PRIMARY KEY,           -- тот же UUID, что раньше жил в users.fsp_id
    linked_at   TIMESTAMPTZ,                -- = users.fsp_linked_at (при желании перенести сюда)
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 1) Наполняем из того, что уже лежит в users
INSERT INTO fsp_profiles (fsp_id, linked_at)
SELECT fsp_id, fsp_linked_at FROM users WHERE fsp_id IS NOT NULL
ON CONFLICT (fsp_id) DO NOTHING;

-- 2) users.fsp_id становится FK на новую таблицу (оставляем nullable — как обсуждали)
ALTER TABLE users
    DROP CONSTRAINT IF EXISTS users_fsp_id_fkey,
    ADD CONSTRAINT users_fsp_id_fkey
        FOREIGN KEY (fsp_id) REFERENCES fsp_profiles(fsp_id);

-- 3) fsp_achievements перенацеливаем с users на fsp_profiles
ALTER TABLE fsp_achievements
    DROP CONSTRAINT IF EXISTS fsp_achievements_fsp_id_fkey,
    ADD CONSTRAINT fsp_achievements_fsp_id_fkey
        FOREIGN KEY (fsp_id) REFERENCES fsp_profiles(fsp_id) ON DELETE CASCADE;


ALTER TABLE candidate_profiles ALTER COLUMN last_name DROP NOT NULL;
ALTER TABLE candidate_profiles ALTER COLUMN first_name DROP NOT NULL;


CREATE OR REPLACE VIEW v_category_rankings AS
SELECT
    cc.category_id,
    cs.candidate_id,
    MAX(ts.percent)                                   AS best_test_percent,
    BOOL_OR(ts.passed)                                AS best_test_passed,
    COALESCE(SUM(fa.weight * CASE WHEN fa.verified THEN 1.5 ELSE 1 END), 0)
                                                      AS fsp_achievements_weight,
    ROUND(
      (COALESCE(MAX(ts.percent),0) * 0.7
       + LEAST(COALESCE(SUM(fa.weight),0), 30)
       + LEAST(COALESCE(MAX(cp.profile_completeness),0) / 10.0, 10)) / 1.1
    , 2)                                              AS rank_score,
    ROW_NUMBER() OVER (
        PARTITION BY cc.category_id
        ORDER BY (COALESCE(MAX(ts.percent),0) * 0.7
                  + LEAST(COALESCE(SUM(fa.weight),0), 30)
                  + LEAST(COALESCE(MAX(cp.profile_completeness),0) / 10.0, 10)) DESC,
                 COALESCE(MAX(ts.percent),0) DESC
    )                                                 AS position_in_category
FROM candidate_categories cc
JOIN candidate_specializations cs
  ON cs.candidate_id = cc.candidate_id
JOIN categories c ON c.id = cc.category_id
 AND cs.specialization_id = c.specialization_id
 AND cs.current_grade_id  = c.grade_id
JOIN candidate_profiles cp ON cp.user_id = cs.candidate_id
LEFT JOIN test_sessions ts
  ON ts.candidate_id = cs.candidate_id
 AND ts.specialization_id = cs.specialization_id
 AND ts.status = 'завершен' AND ts.passed = TRUE
LEFT JOIN users u ON u.id = cs.candidate_id
LEFT JOIN fsp_achievements fa ON fa.fsp_id = u.fsp_id
GROUP BY cc.category_id, cs.candidate_id;


SELECT question_group, count(*) AS variants, min(difficulty), max(difficulty)
FROM test_questions
WHERE specialization_id = 1 AND grade_id = 2 AND is_active = false
GROUP BY question_group;

INSERT INTO statuses (code, entity, name, sort_order)
VALUES ('отменено', 'статус_задания', 'Отменено', 6)
ON CONFLICT (code) DO NOTHING;
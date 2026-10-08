using System.Text.RegularExpressions;
using FspPlatformApi.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FspPlatformApi.Services.Infrastructure;

/// <summary>
/// Автогенерация стандартизированного PDF-профиля кандидата (ТЗ 2.1:
/// «автогенерация стандартизированного PDF-профиля»). Единый шаблон для всех
/// кандидатов — работодатель видит сопоставимый документ, а не произвольные резюме.
///
/// Шаблон: акцентная шапка, ФИО + категория «бейджем», контакты аккуратной колонкой
/// «подпись — значение», секции с акцентными заголовками, навыки — каждый отдельной
/// строкой (без маркеров-«кружочков»). Длинные тексты автоматически перетекают
/// на следующую страницу.
/// </summary>
public static class ProfilePdfGenerator
{
    private const string Accent = "#6d28d9";   // фирменный фиолетовый платформы
    private const string AccentBg = "#f3eefb"; // светлая подложка бейджа
    private const string TextMuted = "#888888";
    private const string TextMain = "#1a1a1a";

    static ProfilePdfGenerator()
        => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] Build(CandidateProfile p, string email,
        string? specializationName, string? gradeName, IReadOnlyList<string> skills,
        bool gradeConfirmed = true)
    {
        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor(TextMain));

            // ---------- Шапка ----------
            page.Header().Column(col =>
            {
                col.Item().Text("Профиль кандидата")
                    .FontSize(22).SemiBold().FontColor(Accent);
                col.Item().Text("Цифровая платформа подбора ИТ-специалистов — ФСП")
                    .FontSize(9).FontColor(TextMuted);
                col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Accent);
            });

            // ---------- Контент ----------
            page.Content().PaddingTop(16).Column(col =>
            {
                col.Spacing(6);

                col.Item().Text(Fio(p)).FontSize(18).SemiBold();

                // Категория — бейджем на светлой подложке
                var category = Category(specializationName, gradeName);
                col.Item().Row(r =>
                {
                    r.AutoItem().Background(AccentBg)
                        .PaddingHorizontal(10).PaddingVertical(5)
                        .Text(category).FontSize(10.5f).SemiBold().FontColor(Accent);
                });
                // Тест на грейд не пройден — честно помечаем, что это заявленный
                // уровень (как в кабинете кандидата и карточке для работодателя).
                if (!gradeConfirmed)
                {
                    col.Item().PaddingTop(2).Text(
                        "Грейд не подтверждён тестированием — указан заявленный уровень.")
                        .FontSize(8.5f).FontColor(TextMuted);
                }

                // Контакты — одна колонка «подпись — значение»: ничего не слипается
                // и не обрезается, порядок чтения естественный
                col.Item().PaddingTop(8).Column(c =>
                {
                    InfoLine(c, "Город", p.City);
                    InfoLine(c, "Стаж", p.ExperienceYears > 0 ? $"{p.ExperienceYears:0.#} лет" : null);
                    InfoLine(c, "Email", email);
                    InfoLine(c, "Телефон", p.Phone);
                    InfoLine(c, "Telegram", p.Telegram);
                });

                var about = Normalize(p.About);
                if (!string.IsNullOrWhiteSpace(about))
                {
                    Section(col, "О себе");
                    col.Item().Text(about);
                }

                // Навыки — каждый отдельной строкой, без маркеров
                if (skills.Count > 0)
                {
                    Section(col, "Стек");
                    col.Item().Column(c =>
                    {
                        foreach (var s in skills)
                            c.Item().PaddingBottom(2).Text(s).SemiBold();
                    });
                }

                if (p.SoftSkills is { Count: > 0 })
                {
                    Section(col, "Софт-скиллы");
                    col.Item().Column(c =>
                    {
                        foreach (var s in p.SoftSkills)
                            c.Item().PaddingBottom(2).Text(s);
                    });
                }

                // Дисклеймер правил видимости (ТЗ 2.2 п.2)
                col.Item().PaddingTop(20).Text(t => t
                    .Span("Контактные данные раскрываются работодателю только после принятия "
                        + "приглашения или отклика. Документ сформирован автоматически из данных профиля.")
                    .FontSize(8).FontColor(TextMuted));
            });

            // ---------- Подвал ----------
            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Сформировано ").FontSize(8).FontColor(TextMuted);
                t.Span(DateTime.Now.ToString("dd.MM.yyyy")).FontSize(8).FontColor(TextMuted);
                t.Span("   \u00B7   стр. ").FontSize(8).FontColor(TextMuted);
                t.CurrentPageNumber().FontSize(8).FontColor(TextMuted);
            });
        })).GeneratePdf();
    }

    /// <summary>Заголовок секции: акцентный текст + разделительная линия.</summary>
    private static void Section(ColumnDescriptor col, string title)
    {
        col.Item().PaddingTop(10).Row(r =>
        {
            r.AutoItem().Text(title).SemiBold().FontColor(Accent).FontSize(11);
            r.RelativeItem().PaddingLeft(8).PaddingTop(7)
                .LineHorizontal(0.5f).LineColor("#dddddd");
        });
    }

    /// <summary>Строка «подпись — значение» в блоке контактов.</summary>
    private static void InfoLine(ColumnDescriptor c, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        c.Item().PaddingBottom(4).Row(r =>
        {
            r.ConstantItem(70).Text(label).FontSize(8.5f).FontColor(TextMuted);
            r.RelativeItem().Text(value).SemiBold();
        });
    }

    /// <summary>
    /// «Рассклейка» текста перед вёрсткой: в данных профиля могли остаться
    /// слитые токены из старых импортов резюме («ОПЫТРАБОТЫ2024», «Python.Быстро»,
    /// «Rail,баг»). Вставляем пробел только на очевидных границах — смысл текста
    /// не меняется, а читаемость PDF восстанавливается.
    /// </summary>
    private static string? Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var t = text.Replace('\r', ' ').Replace('\n', ' ');
        t = Regex.Replace(t, @"(?<=[,.;:!?])(?=\S)", " ");          // точка/запятая → слово
        t = Regex.Replace(t, @"(?<=[а-яё])(?=[А-ЯЁ])", " ");          // стык регистров кириллицы
        t = Regex.Replace(t, @"(?<=[А-ЯЁ])(?=[A-Za-z])|(?<=[A-Za-z])(?=[А-ЯЁ])", " "); // смена алфавита
        t = Regex.Replace(t, @"(?<=[A-Za-zА-Яа-яЁё])(?=\d)", " ");   // буква → цифра (РАБОТЫ2024)
        t = Regex.Replace(t, @"\s+", " ");
        return t.Trim();
    }

    private static string Fio(CandidateProfile p)
        => string.Join(" ", new[] { p.LastName, p.FirstName, p.MiddleName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string Category(string? spec, string? grade)
        => (spec, grade) switch
        {
            (null, null) => "категория не определена (тест не пройден)",
            (not null, null) => spec,
            (null, not null) => grade,
            _ => $"{spec} \u00B7 {grade}"
        };
}
using ICSharpCode.Decompiler.CSharp.Syntax;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Shatbly.Reports;

public class SimpleReport : IDocument
{
    private readonly IReadOnlyList<User> _users;
    private readonly DateTime _generatedAt;

    public SimpleReport(IEnumerable<User> users)
    {
        _users = users
            .OrderByDescending(user => user.Id)
            .ThenBy(user => user.Name)
            .ToList();

        _generatedAt = DateTime.Now;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = "Shatbly Users Report",
        Author = "Shatbly Admin Dashboard",
        Subject = "Detailed user analytics report",
        Keywords = "users,roles,orders,analytics,shatbly",
        CreationDate = _generatedAt
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(text => text.FontSize(9).FontColor(ReportColors.Text));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(14).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container
            .Background(ReportColors.Navy)
            .Padding(16)
            .Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Shatbly")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.White);

                    column.Item().PaddingTop(4).Text("Complex Users Analytics Report")
                        .FontSize(12)
                        .FontColor(ReportColors.MutedLight);
                });

                row.ConstantItem(210).AlignRight().Column(column =>
                {
                    column.Item().AlignRight().Text($"Generated: {_generatedAt:dd MMM yyyy, hh:mm tt}")
                        .FontSize(9)
                        .FontColor(Colors.White);

                    column.Item().PaddingTop(4).AlignRight().Text($"Total records: {_users.Count:N0}")
                        .FontSize(9)
                        .FontColor(ReportColors.MutedLight);
                });
            });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            column.Item().Element(ComposeExecutiveSummary);

            column.Item().Row(row =>
            {
                row.RelativeItem(3).Element(ComposeRoleDistribution);
                row.ConstantItem(14);
                row.RelativeItem(4).Element(ComposeMonthlyTrend);
            });

            column.Item().Element(ComposeUsersTable);
        });
    }

    private void ComposeExecutiveSummary(IContainer container)
    {
        var totalUsers = _users.Count;
        var newThisMonth = _users.Count(user => IsSameMonth(user.CreatedAt, _generatedAt));
        var customers = CountRole(SD.ROLE_CUSTOMER);
        var admins = CountRole(SD.ROLE_ADMIN);
        var workers = CountRole(SD.ROLE_WORKER);
        var SuperAdmins = CountRole(SD.ROLE_SUPER_ADMIN);
        var totalOrders = _users.Sum(user => SafeCount(user.ClientBookings) + SafeCount(user.ClientBookings));

        container.Row(row =>
        {
            row.Spacing(10);

            row.RelativeItem().Element(Card).Column(column =>
            {
                column.Item().Text("Total Users").FontColor(ReportColors.Muted).FontSize(8);
                column.Item().Text($"{totalUsers:N0}").Bold().FontSize(22).FontColor(ReportColors.Navy);
                column.Item().Text("Registered accounts").FontColor(ReportColors.Muted).FontSize(8);
            });

            row.RelativeItem().Element(Card).Column(column =>
            {
                column.Item().Text("New This Month").FontColor(ReportColors.Muted).FontSize(8);
                column.Item().Text($"{newThisMonth:N0}").Bold().FontSize(22).FontColor(ReportColors.Green);
                column.Item().Text($"{Percent(newThisMonth, totalUsers):0.#}% of all users").FontColor(ReportColors.Muted).FontSize(8);
            });

            row.RelativeItem().Element(Card).Column(column =>
            {
                column.Item().Text("Customers").FontColor(ReportColors.Muted).FontSize(8);
                column.Item().Text($"{customers:N0}").Bold().FontSize(22).FontColor(ReportColors.Blue);
                column.Item().Text($"{Percent(customers, totalUsers):0.#}% share").FontColor(ReportColors.Muted).FontSize(8);
            });

            row.RelativeItem().Element(Card).Column(column =>
            {
                column.Item().Text("Workers").FontColor(ReportColors.Muted).FontSize(8);
                column.Item().Text($"{workers:N0}").Bold().FontSize(22).FontColor(ReportColors.Orange);
                column.Item().Text($"{Percent(workers, totalUsers):0.#}% share").FontColor(ReportColors.Muted).FontSize(8);
            });

            row.RelativeItem().Element(Card).Column(column =>
            {
                column.Item().Text("Admins").FontColor(ReportColors.Muted).FontSize(8);
                column.Item().Text($"{admins:N0}").Bold().FontSize(22).FontColor(ReportColors.Red);
                column.Item().Text($"{Percent(admins, totalUsers):0.#}% share").FontColor(ReportColors.Muted).FontSize(8);
            });

            row.RelativeItem().Element(Card).Column(column =>
            {
                column.Item().Text("Order Links").FontColor(ReportColors.Muted).FontSize(8);
                column.Item().Text($"{totalOrders:N0}").Bold().FontSize(22).FontColor(ReportColors.Purple);
                column.Item().Text("Created + assigned").FontColor(ReportColors.Muted).FontSize(8);
            });
        });

        static IContainer Card(IContainer item)
        {
            return item
                .Border(1)
                .BorderColor(ReportColors.Border)
                .Background(ReportColors.Panel)
                .Padding(10);
        }
    }

    private void ComposeRoleDistribution(IContainer container)
    {
        var roleGroups = _users
            .GroupBy(user => string.IsNullOrWhiteSpace(user.NormalizedUserName) ? "Unknown" : user.NormalizedUserName)
            .Select(group => new ChartPoint(group.Key, group.Count()))
            .OrderByDescending(point => point.Value)
            .ThenBy(point => point.Label)
            .ToList();

        container.Element(Panel).Column(column =>
        {
            column.Spacing(9);
            column.Item().Element(section => ComposeSectionTitle(section, "Role Distribution", "Users grouped by application role"));

            if (roleGroups.Count == 0)
            {
                column.Item().Text("No role data available.").FontColor(ReportColors.Muted);
                return;
            }

            var max = roleGroups.Max(point => point.Value);

            foreach (var point in roleGroups)
                column.Item().Element(item => ComposeBar(item, point.Label, point.Value, max, RoleColor(point.Label)));
        });
    }

    private void ComposeMonthlyTrend(IContainer container)
    {
        var firstMonth = _generatedAt.AddMonths(-5);
        var monthBuckets = Enumerable.Range(0, 6)
            .Select(offset => new DateTime(firstMonth.Year, firstMonth.Month, 1).AddMonths(offset))
            .Select(month => new ChartPoint(
                month.ToString("MMM yyyy"),
                _users.Count(user => user.CreatedAt.Year == month.Year && user.CreatedAt.Month == month.Month)))
            .ToList();

        container.Element(Panel).Column(column =>
        {
            column.Spacing(10);
            column.Item().Element(section => ComposeSectionTitle(section, "Registration Trend", "New users across the last 6 months"));

            var max = Math.Max(1, monthBuckets.Max(point => point.Value));

            column.Item().Height(138).Row(row =>
            {
                row.Spacing(8);

                foreach (var point in monthBuckets)
                {
                    var height = 18 + (point.Value / (float)max * 92);

                    row.RelativeItem().AlignBottom().Column(bar =>
                    {
                        bar.Item().AlignCenter().Text($"{point.Value:N0}")
                            .Bold()
                            .FontSize(8)
                            .FontColor(ReportColors.Navy);

                        bar.Item().Height(height)
                            .Background(ReportColors.Blue)
                            .Border(1)
                            .BorderColor(ReportColors.BlueDark);

                        bar.Item().PaddingTop(5).AlignCenter().Text(point.Label)
                            .FontSize(7)
                            .FontColor(ReportColors.Muted);
                    });
                }
            });
        });
    }

    private void ComposeUsersTable(IContainer container)
    {
        container.Element(Panel).Column(column =>
        {
            column.Spacing(10);
            column.Item().Element(section => ComposeSectionTitle(section, "User Directory", "Detailed account, contact, role, and order activity"));

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(32);
                    columns.RelativeColumn(1.6f);
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.8f);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "#");
                    HeaderCell(header, "Name");
                    HeaderCell(header, "Email");
                    HeaderCell(header, "Phone");
                    HeaderCell(header, "Role");
                    HeaderCell(header, "Created");
                    HeaderCell(header, "Orders");
                    HeaderCell(header, "Assigned");
                });

                if (_users.Count == 0)
                {
                    table.Cell().ColumnSpan(8).Element(EmptyCell).Text("No users found for this report.");
                    return;
                }

                for (var index = 0; index < _users.Count; index++)
                {
                    var user = _users[index];
                    var background = index % 2 == 0 ? Colors.White.ToString() : ReportColors.RowAlt.ToString();

                    BodyCell(table, background).Text($"{index + 1:N0}");
                    BodyCell(table, background).Text(Coalesce(user.Name));
                    BodyCell(table, background).Text(Coalesce(user.Email));
                    BodyCell(table, background).Text(Coalesce(user.Phone));
                    BodyCell(table, background).Text(Coalesce(user.NormalizedUserName));
                    BodyCell(table, background).Text(user.CreatedAt.ToString("dd MMM yyyy"));
                    BodyCell(table, background).AlignRight().Text($"{SafeCount(user.Orders):N0}");
                    BodyCell(table, background).AlignRight().Text($"{SafeCount(user.ClientBookings):N0}");
                }
                // التأكد من أن القائمة ليست فارغة لتجنب خطأ NullReference
                //if (_users != null)
                //{
                //    for (var index = 0; index < _users.Count; index++)
                //    {
                //        var user = _users[index];

                //        // لو المستخدم نفسه Null، نتخطى هذه اللفة
                //        if (user == null) continue;

                //        var background = index % 2 == 0 ? Colors.White.ToString() : ReportColors.RowAlt.ToString();
                //        BodyCell(table, background).Text($"{index + 1:N0}");
                //        BodyCell(table, background).Text(Coalesce(user.Name));
                //        BodyCell(table, background).Text(Coalesce(user.Email));
                //        BodyCell(table, background).Text(Coalesce(user.Phone));
                //        BodyCell(table, background).Text(Coalesce(user.NormalizedUserName));

                //        // استخدام عامل الحماية '?' في حال كان المتغير Nullable
                //        BodyCell(table, background).Text(user.CreatedAt?.ToString("dd MMM yyyy") ?? "-");

                //        BodyCell(table, background).AlignRight().Text($"{SafeCount(user.Orders):N0}");

                //        // تم حذف السطر المكرر الخاص بـ SafeCount(user.Orders)
                //        // إذا كان هذا السطر مخصصاً لعمود آخر (مثل إجمالي المبيعات)، تأكد من تغييره إلى المتغير الصحيح.
                //    }
                //}
            });
        });
    }


    private static void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell()
            .Background(ReportColors.Navy)
            .BorderRight(1)
            .BorderColor(Colors.White)
            .PaddingVertical(7)
            .PaddingHorizontal(6)
            .Text(text)
            .Bold()
            .FontSize(8)
            .FontColor(Colors.White);
    }

    private void ComposeFooter(IContainer container)
    {
        container
            .BorderTop(1)
            .BorderColor(ReportColors.Border)
            .PaddingTop(8)
            .Row(row =>
            {
                row.RelativeItem().Text("Shatbly Admin Dashboard")
                    .FontSize(8)
                    .FontColor(ReportColors.Muted);

                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(ReportColors.Muted));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });

                row.RelativeItem().AlignRight().Text("Confidential internal report")
                    .FontSize(8)
                    .FontColor(ReportColors.Muted);
            });
    }

    private static void ComposeSectionTitle(IContainer container, string title, string subtitle)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(title)
                    .FontSize(13)
                    .Bold()
                    .FontColor(ReportColors.Navy);

                column.Item().Text(subtitle)
                    .FontSize(8)
                    .FontColor(ReportColors.Muted);
            });
        });
    }

    private static void ComposeBar(IContainer container, string label, int value, int max, string color)
    {
        var percentage = max == 0 ? 0 : value / (float)max;

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(label).FontSize(8).FontColor(ReportColors.Text);
                row.ConstantItem(46).AlignRight().Text($"{value:N0}").Bold().FontSize(8).FontColor(ReportColors.Navy);
            });

            column.Item().PaddingTop(3).Background(ReportColors.BarTrack).Height(9).Row(row =>
            {
                row.RelativeItem(Math.Max(percentage, 0.02f)).Background(color);
                row.RelativeItem(Math.Max(1 - percentage, 0.001f));
            });
        });
    }

    private static IContainer Panel(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(ReportColors.Border)
            .Padding(12);
    }

    private static void HeaderCell(TableDescriptor header, string text)
    {
        header.Cell()
            .Background(ReportColors.Navy)
            .BorderRight(1)
            .BorderColor(Colors.White)
            .PaddingVertical(7)
            .PaddingHorizontal(6)
            .Text(text)
            .Bold()
            .FontSize(8)
            .FontColor(Colors.White);
    }

    private static IContainer BodyCell(TableDescriptor table, string background)
    {
        return table.Cell()
            .Background(background)
            .BorderBottom(1)
            .BorderColor(ReportColors.Border)
            .PaddingVertical(6)
            .PaddingHorizontal(6);
    }

    private static IContainer EmptyCell(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(ReportColors.Border)
            .Padding(18)
            .AlignCenter();
    }

    private int CountRole(string role)
    {
        return _users.Count(user => string.Equals(user.NormalizedUserName, role, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSameMonth(DateTime value, DateTime comparison)
    {
        return value.Year == comparison.Year && value.Month == comparison.Month;
    }

    private static int SafeCount<T>(ICollection<T>? collection)
    {
        return collection?.Count ?? 0;
    }

    private static decimal Percent(int value, int total)
    {
        return total == 0 ? 0 : value * 100m / total;
    }

    private static string Coalesce(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string RoleColor(string role)
    {
        if (string.Equals(role, SD.ROLE_ADMIN, StringComparison.OrdinalIgnoreCase))
            return ReportColors.Red;

        if (string.Equals(role, SD.ROLE_WORKER, StringComparison.OrdinalIgnoreCase))
            return ReportColors.Orange;

        if (string.Equals(role, SD.ROLE_CUSTOMER, StringComparison.OrdinalIgnoreCase))
            return ReportColors.Blue;

        return ReportColors.Purple;
    }

    private sealed record ChartPoint(string Label, int Value);

    private static class ReportColors
    {
        public const string Navy = "#172033";
        public const string Text = "#232936";
        public const string Muted = "#687083";
        public const string MutedLight = "#CBD5E1";
        public const string Panel = "#F8FAFC";
        public const string Border = "#D9E2EC";
        public const string RowAlt = "#F6F8FB";
        public const string BarTrack = "#E8EEF5";
        public const string Blue = "#2563EB";
        public const string BlueDark = "#1D4ED8";
        public const string Green = "#0F9F6E";
        public const string Orange = "#D97706";
        public const string Red = "#DC2626";
        public const string Purple = "#7C3AED";
    }
}

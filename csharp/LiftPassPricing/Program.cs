using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;

Console.WriteLine(@"LiftPassPricing Api started on 5000,
you can open http://localhost:5000/prices?type=night&age=23&date=2019-02-18 in a navigator
and you'll get the price of the list pass for the day.");

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var connection = new MySqlConnection
{
    ConnectionString = @"Database=lift_pass;Data Source=localhost;User Id=root;Password=mysql"
};
connection.Open();

app.MapPut("/prices", (int cost, string type) =>
{
    using (var command = new MySqlCommand( //
               "INSERT INTO base_price (type, cost) VALUES (@type, @cost) " + //
               "ON DUPLICATE KEY UPDATE cost = @cost;", connection))
    {
        command.Parameters.AddWithValue("@type", type);
        command.Parameters.AddWithValue("@cost", cost);
        command.Prepare();
        command.ExecuteNonQuery();
    }

    return "";
});

app.MapGet("/prices", (HttpContext context) =>
{
    int? age = context.Request.Query["age"].Count != 0 ? Int32.Parse(context.Request.Query["age"][0]) : null;

    using (var costCmd = new MySqlCommand( //
               "SELECT cost FROM base_price " + //
               "WHERE type = @type", connection))
    {
        costCmd.Parameters.AddWithValue("@type", context.Request.Query["type"][0]);
        costCmd.Prepare();
        double result = (int)costCmd.ExecuteScalar();

        int reduction;
        var isHoliday = false;

        if (age != null && age < 6)
        {
            return Results.Text("{ \"cost\": 0}","application/json");
        }
        else
        {
            reduction = 0;

            if (!"night".Equals(context.Request.Query["type"][0]))
            {
                using (var holidayCmd = new MySqlCommand( //
                           "SELECT * FROM holidays", connection))
                {
                    holidayCmd.Prepare();
                    using (var holidays = holidayCmd.ExecuteReader())
                    {
                        while (holidays.Read())
                        {
                            var holiday = holidays.GetDateTime("holiday");
                            if (context.Request.Query["date"].Count != 0)
                            {
                                DateTime d = DateTime.ParseExact(context.Request.Query["date"][0], "yyyy-MM-dd",
                                    CultureInfo.InvariantCulture);
                                if (d.Year == holiday.Year &&
                                    d.Month == holiday.Month &&
                                    d.Date == holiday.Date)
                                {
                                    isHoliday = true;
                                }
                            }
                        }
                    }
                }

                if (context.Request.Query["date"].Count != 0)
                {
                    DateTime d = DateTime.ParseExact(context.Request.Query["date"][0], "yyyy-MM-dd",
                        CultureInfo.InvariantCulture);
                    if (!isHoliday && (int)d.DayOfWeek == 1)
                    {
                        reduction = 35;
                    }
                }

                // TODO apply reduction for others
                if (age != null && age < 15)
                {
                    return Results.Text("{ \"cost\": " + (int)Math.Ceiling(result * .7) + "}","application/json");
                }
                else
                {
                    if (age == null)
                    {
                        double cost = result * (1 - reduction / 100.0);
                        return Results.Text("{ \"cost\": " + (int)Math.Ceiling(cost) + "}","application/json");
                    }
                    else
                    {
                        if (age > 64)
                        {
                            double cost = result * .75 * (1 - reduction / 100.0);
                            return Results.Text("{ \"cost\": " + (int)Math.Ceiling(cost) + "}","application/json");
                        }
                        else
                        {
                            double cost = result * (1 - reduction / 100.0);
                            return Results.Text("{ \"cost\": " + (int)Math.Ceiling(cost) + "}","application/json");
                        }
                    }
                }
            }
            else
            {
                if (age != null && age >= 6)
                {
                    if (age > 64)
                    {
                        return Results.Text("{ \"cost\": " + (int)Math.Ceiling(result * .4) + "}","application/json");
                    }
                    else
                    {
                        return Results.Text("{ \"cost\": " + result + "}","application/json");
                    }
                }
                else
                {
                    return Results.Text("{ \"cost\": 0}","application/json");
                }
            }
        }
    }
});
app.MapGet("/", () => "Hello World!");
app.Run();

public partial class Program
{
}
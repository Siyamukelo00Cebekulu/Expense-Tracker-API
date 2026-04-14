using ExpenseTrackerApi.Data;
using ExpenseTrackerApi.DTOs;
using ExpenseTrackerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExpenseTrackerApi.Controllers;

[ApiController]
[Route("/api/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExpensesController(AppDbContext db)
    {
        _db = db;
    }

    private int? UserId
    {
        get
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return int.TryParse(claimValue, out var id) ? id : null;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetExpenses(string? filter, DateTime? startDate, DateTime? endDate)
    {
        if (UserId is null)
        {
            return Unauthorized();
        }

        var query = _db.Expenses.Where(e => e.UserId == UserId.Value).AsQueryable();
        var today = DateTime.UtcNow.Date;

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = filter.ToLowerInvariant() switch
            {
                "past-week" => query.Where(e => e.Date >= today.AddDays(-7) && e.Date <= today),
                "past-month" => query.Where(e => e.Date >= today.AddMonths(-1) && e.Date <= today),
                "past-3-months" => query.Where(e => e.Date >= today.AddMonths(-3) && e.Date <= today),
                "custom" when startDate.HasValue && endDate.HasValue => query.Where(e => e.Date >= startDate.Value.Date && e.Date <= endDate.Value.Date),
                "custom" => query.Where(e => false),
                _ => query
            };
        }

        var results = await query
            .OrderByDescending(e => e.Date)
            .Select(e => new ExpenseResponse
            {
                Id = e.Id,
                Title = e.Title,
                Amount = e.Amount,
                Category = e.Category,
                Date = e.Date,
                Notes = e.Notes
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpense(CreateExpenseRequest request)
    {
        if (UserId is null)
        {
            return Unauthorized();
        }

        var expense = new Expense
        {
            Title = request.Title.Trim(),
            Amount = request.Amount,
            Category = request.Category,
            Date = request.Date.Date,
            Notes = request.Notes?.Trim(),
            UserId = UserId.Value
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        var response = new ExpenseResponse
        {
            Id = expense.Id,
            Title = expense.Title,
            Amount = expense.Amount,
            Category = expense.Category,
            Date = expense.Date,
            Notes = expense.Notes
        };

        return Created($"/api/expenses/{expense.Id}", response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, UpdateExpenseRequest request)
    {
        if (UserId is null)
        {
            return Unauthorized();
        }

        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == UserId.Value);
        if (expense is null)
        {
            return NotFound();
        }

        expense.Title = request.Title.Trim();
        expense.Amount = request.Amount;
        expense.Category = request.Category;
        expense.Date = request.Date.Date;
        expense.Notes = request.Notes?.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        if (UserId is null)
        {
            return Unauthorized();
        }

        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == UserId.Value);
        if (expense is null)
        {
            return NotFound();
        }

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

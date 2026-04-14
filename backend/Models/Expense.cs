namespace ExpenseTrackerApi.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum ExpenseCategory
{
    Groceries,
    Leisure,
    Electronics,
    Utilities,
    Clothing,
    Health,
    Others
}

public class Expense
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    public ExpenseCategory Category { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    public User? User { get; set; }
}

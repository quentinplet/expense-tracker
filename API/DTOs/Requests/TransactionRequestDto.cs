using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.Entities;

namespace API.DTOs.Requests;

public class TransactionRequestDto
{
    [Required(ErrorMessage = "The Transaction Amount is required")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "The Transaction Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    // TransactionResponseDto.Type est une string : le client relit « Expense » /
    // « Income » et les renvoie tels quels. Sans ce convertisseur, la lecture et
    // l'écriture ne parlaient pas la même langue — le POST échouait en 400 sur
    // « The JSON value could not be converted to TransactionType ».
    [Required(ErrorMessage = "The Transaction Type is required")]
    [EnumDataType(typeof(TransactionType))]
    [JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
    public TransactionType? Type { get; set; }

    [Required(ErrorMessage = "The Transaction Date is required")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "The Transaction Label is required")]
    [StringLength(200, ErrorMessage = "The Transaction Label must be 200 characters or fewer")]
    public string? Label { get; set; }

    public string? Note { get; set; }

    [Required(ErrorMessage = "The Transaction Category is required")]
    public Guid? CategoryId { get; set; }
}
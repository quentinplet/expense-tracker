using System;
using System.ComponentModel.DataAnnotations;
using System.Transactions;
using API.Entities;

namespace API.DTOs.Requests;

public class CategoryRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public required string Name { get; set; }

    [Required]
    [EnumDataType(typeof(TransactionTypeName))]
    public TransactionTypeName TransactionType { get; set; }

}

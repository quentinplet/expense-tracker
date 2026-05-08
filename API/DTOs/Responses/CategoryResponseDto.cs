using System;
using API.Entities;

namespace API.DTOs.Responses;

public class CategoryResponseDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool Enabled { get; set; }
    public TransactionTypeName TransactionTypeName { get; set; }

}

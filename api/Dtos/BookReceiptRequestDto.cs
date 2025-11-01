using System;
using System.Collections.Generic;

namespace LexiFlow.Api.Dtos;

public record BookReceiptRequestDto(Guid ReceiptId, Dictionary<string, string>? Corrections);

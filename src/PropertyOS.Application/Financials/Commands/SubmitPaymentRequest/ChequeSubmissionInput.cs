using System;

namespace PropertyOS.Application.Financials.Commands.SubmitPaymentRequest;

public record ChequeSubmissionInput(
    string ChequeNumber,
    string BankName,
    DateOnly IssueDate,
    DateOnly DueDate);

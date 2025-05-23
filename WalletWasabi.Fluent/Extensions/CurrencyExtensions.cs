using System.Globalization;
using System.Linq;
using NBitcoin;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Extensions;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.Models;

namespace WalletWasabi.Fluent.Extensions;

public static class CurrencyExtensions
{
	private static readonly NumberFormatInfo FormatInfo = new()
	{
		CurrencyGroupSeparator = " ",
		NumberGroupSeparator = " ",
		CurrencyDecimalSeparator = ".",
		NumberDecimalSeparator = "."
	};

	public static Money CalculateDestinationAmount(this BuildTransactionResult result, Destination destination)
	{
		var isNormalPayment = result.OuterWalletOutputs.Any();

		if (isNormalPayment)
		{
			return result.OuterWalletOutputs.Sum(x => x.Amount);
		}

		return result.InnerWalletOutputs
			.Where(x =>
			destination is Destination.Loudly loudly && x.ScriptPubKey == loudly.ScriptPubKey
			/* TODO add info to results about what is the change*/)
			.Sum(x => x.Amount);
	}

	public static string FormattedBtc(this decimal amount)
	{
		return string.Format(FormatInfo, "{0:### ### ### ##0.## ### ###}", amount).Trim();
	}

	public static string FormattedBtcFixedFractional(this decimal amount)
	{
		return string.Format(FormatInfo, "{0:### ### ### ##0.00 000 000}", amount).Trim();
	}

	public static string FormattedBtcExactFractional(this decimal amount, int fractionalDigits)
	{
		fractionalDigits = Math.Min(fractionalDigits, 8);
		var fractionalFormat = new string('0', fractionalDigits);
		if (fractionalFormat.Length > 4)
		{
			fractionalFormat = fractionalFormat.Insert(4, " ");
		}

		var fullFormat = $"{{0:### ### ### ##0.{fractionalFormat}}}";

		return string.Format(FormatInfo, fullFormat, amount).Trim();
	}

	public static string FormattedBtcExactFractional(this decimal amount, string originalText)
	{
		var fractionalCount =
			originalText.Contains('.')
			? originalText.Skip(originalText.LastIndexOf('.')).Where(char.IsDigit).Count()
			: 0;

		return FormattedBtcExactFractional(amount, fractionalCount);
	}

	public static string FormattedFiat(this decimal amount, string format = "N2")
	{
		return amount.ToString(format, FormatInfo).Trim();
	}

	public static decimal BtcToUsd(this Money money, decimal exchangeRate)
	{
		return money.ToDecimal(MoneyUnit.BTC) * exchangeRate;
	}

	public static string ToUsdAprox(this decimal n) => n != decimal.Zero ? $"≈{ToUsdFormatted(n)}" : "";

	public static string ToUsdAproxBetweenParens(this decimal n) => n != decimal.Zero ? $"({ToUsdAprox(n)})" : "";

	public static string ToUsdFormatted(this decimal n)
	{
		return ToUsdAmountFormatted(n) + " USD";
	}

	public static string ToUsdAmountFormatted(this decimal n)
	{
		return n switch
		{
			>= 10 => Math.Ceiling(n).ToString("N0", FormatInfo),
			>= 1 => n.ToString("N1", FormatInfo),
			_ => n.ToString("N2", FormatInfo)
		};
	}

	public static string ToUsd(this decimal n)
	{
		return n.WithFriendlyDecimals() + " USD";
	}

	public static decimal WithFriendlyDecimals(this double n)
	{
		return WithFriendlyDecimals((decimal) n);
	}

	public static decimal WithFriendlyDecimals(this decimal n)
	{
		return Math.Abs(n) switch
		{
			>= 10 => decimal.Round(n),
			>= 1 => decimal.Round(n, 1),
			_ => decimal.Round(n, 2)
		};
	}

	public static string ToFeeDisplayUnitRawString(this Money? fee) =>
		fee is null ? "Unknown" : fee.ToString();

}

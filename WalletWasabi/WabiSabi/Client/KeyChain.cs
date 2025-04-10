using NBitcoin;
using System.Linq;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Crypto;
using WalletWasabi.Extensions;
using WalletWasabi.Wallets;

namespace WalletWasabi.WabiSabi.Client;

public class KeyChain : IKeyChain
{
	public KeyChain(KeyManager keyManager, string password)
	{
		if (keyManager.IsWatchOnly)
		{
			throw new ArgumentException("A watch-only key manager cannot be used to initialize a key chain.");
		}

		_keyManager = keyManager;
		_password = password;
	}

	private readonly KeyManager _keyManager;
	private readonly string _password;

	private Key GetMasterKey()
	{
		return _keyManager.GetMasterExtKey(_password).PrivateKey;
	}

	public OwnershipProof GetOwnershipProof(IDestination destination, CoinJoinInputCommitmentData commitmentData)
	{
		Key key = _keyManager.GetSecrets(_password, destination.ScriptPubKey).SingleOrDefault()
			?? throw new InvalidOperationException($"The signing key for '{destination.ScriptPubKey}' was not found.");
		Key masterKey = GetMasterKey();
		BitcoinSecret secret = key.GetBitcoinSecret(_keyManager.GetNetwork(), destination.ScriptPubKey);

		return NBitcoinExtensions.GetOwnershipProof(masterKey, secret, destination.ScriptPubKey, commitmentData);
	}

	public Transaction Sign(Transaction transaction, Coin coin, PrecomputedTransactionData precomputedTransactionData)
	{
		transaction = transaction.Clone();

		if (transaction.Inputs.Count == 0)
		{
			throw new ArgumentException("No inputs to sign.", nameof(transaction));
		}

		var txInput = transaction.Inputs.AsIndexedInputs().FirstOrDefault(input => input.PrevOut == coin.Outpoint)
			?? throw new InvalidOperationException("Missing input.");
		Key key = _keyManager.GetSecrets(_password, coin.ScriptPubKey).SingleOrDefault()
			?? throw new InvalidOperationException($"The signing key for '{coin.ScriptPubKey}' was not found.");
		BitcoinSecret secret = key.GetBitcoinSecret(_keyManager.GetNetwork(), coin.ScriptPubKey);

		TransactionBuilder builder = Network.Main.CreateTransactionBuilder();
		builder.AddKeys(secret);
		builder.AddCoins(coin);
		builder.SetSigningOptions(new SigningOptions(TaprootSigHash.All, (TaprootReadyPrecomputedTransactionData)precomputedTransactionData));
		builder.SignTransactionInPlace(transaction);

		return transaction;
	}
}

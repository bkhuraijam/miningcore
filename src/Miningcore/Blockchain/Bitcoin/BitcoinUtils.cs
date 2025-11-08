using System.Diagnostics;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Miningcore.Blockchain.Bitcoin;

public static class BitcoinUtils
{
    /// <summary>
    /// Convert address strings into IDestination objects for different Bitcoin-family coins.
    /// </summary>
    public static IDestination AddressToDestination(string address, Network expectedNetwork)
    {
        // Handle eCash CashAddr explicitly
        if(address.StartsWith("ecash:", StringComparison.OrdinalIgnoreCase) ||
           address.StartsWith("q", StringComparison.OrdinalIgnoreCase) ||
           address.StartsWith("p", StringComparison.OrdinalIgnoreCase))
        {
            return ECashAddressToDestination(address);
        }

        // Default Base58 handling (BTC, etc.)
        var decoded = Encoders.Base58Check.DecodeData(address);
        var networkVersionBytes = expectedNetwork.GetVersionBytes(Base58Type.PUBKEY_ADDRESS, true);
        decoded = decoded.Skip(networkVersionBytes.Length).ToArray();
        var result = new KeyId(decoded);

        return result;
    }

    /// <summary>
    /// Dedicated eCash CashAddr decoder.
    /// </summary>
    public static IDestination ECashAddressToDestination(string address)
    {
        // Normalize ecash: prefix to bitcoincash: for compatibility
        if(address.StartsWith("ecash:", StringComparison.OrdinalIgnoreCase))
            address = "bitcoincash:" + address.Substring("ecash:".Length);
        else
            address = "bitcoincash:" + address;

        // Use BCH CashAddr parser but ignore network mismatch
        var bcashNet = NBitcoin.Altcoins.BCash.Instance.Mainnet;
        var cashAddr = bcashNet.Parse<NBitcoin.Altcoins.BCash.BTrashPubKeyAddress>(address);

        // Return destination against Network.Main to bypass strict BCH validation
        return cashAddr.ScriptPubKey.GetDestinationAddress(Network.Main);
    }

    public static IDestination BechSegwitAddressToDestination(string address, Network expectedNetwork, string bechPrefix)
    {
        var encoder = Encoders.Bech32(bechPrefix);
        var decoded = encoder.Decode(address, out var witVersion);
        var result = new WitKeyId(decoded);

        Debug.Assert(result.GetAddress(expectedNetwork).ToString() == address);
        return result;
    }

    public static IDestination BCashAddressToDestination(string address, Network expectedNetwork)
    {
        // Default BCH handling
        var bcash = NBitcoin.Altcoins.BCash.Instance.GetNetwork(expectedNetwork.ChainName);
        var trashAddress = bcash.Parse<NBitcoin.Altcoins.BCash.BTrashPubKeyAddress>(address);
        return trashAddress.ScriptPubKey.GetDestinationAddress(bcash);
    }

    public static IDestination LitecoinAddressToDestination(string address, Network expectedNetwork)
    {
        var litecoin = NBitcoin.Altcoins.Litecoin.Instance.GetNetwork(expectedNetwork.ChainName);
        var encoder = litecoin.GetBech32Encoder(Bech32Type.WITNESS_PUBKEY_ADDRESS, true);

        var decoded = encoder.Decode(address, out var witVersion);
        var result = new WitKeyId(decoded);

        Debug.Assert(result.GetAddress(litecoin).ToString() == address);
        return result;
    }
}

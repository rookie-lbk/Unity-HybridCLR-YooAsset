using YooAsset.Editor;

public class AddressByPathNoPerfix : IAddressRule
{
    string IAddressRule.GetAssetAddress(AddressRuleData data)
    {
        return data.AssetPath.Replace("Assets/Samples/Space Shooter/", "");
    }
}
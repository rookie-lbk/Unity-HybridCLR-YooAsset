using YooAsset.Editor;

public class AddressByPathNoPerfix : IAddressRule
{
    string IAddressRule.GetAssetAddress(AddressRuleData data)
    {
        return data.AssetPath.Replace("Assets/GameAssets/Res/", "");
    }
}
public class DLLAddressByPathNoPerfix : IAddressRule
{
    string IAddressRule.GetAssetAddress(AddressRuleData data)
    {
        return data.AssetPath.Replace("Assets/GameAssets/DLLs/", "");
    }
}
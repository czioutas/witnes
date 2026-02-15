public enum PayloadReason
{
    UncompressedAssets = 1,   // isCompressed is false for large assets
    ExcessiveSize = 2,        // Total KB exceeds threshold
    ScriptBloat = 3,           // JS specifically is too heavy
    LargeApiResponses = 4,
    ConcurrencyBottleneck = 5
}




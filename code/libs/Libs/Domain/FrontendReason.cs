using System.ComponentModel;
using System.Runtime.Serialization;
using Libs.Converters;

namespace Libs.Domain;

[TypeConverter(typeof(EnumTypeConverter<FrontendReason>))]
public enum FrontendReason
{
    [EnumMember(Value = "heavy_app_bundle")]
    HeavyAppBundle = 1,        // High Shell-to-Content Gap (LCP - FCP)

    [EnumMember(Value = "hydration_lockup")]
    HydrationLockup = 2,       // High Frozen UI Gap (Interactive - LCP)

    [EnumMember(Value = "zombie_ui")]
    ZombieUI = 3,              // High Interaction Dead Zone (Functional - Technical)

    [EnumMember(Value = "implementation_layout_shift")]
    ImplementationLayoutShift = 4, // High CLS caused by coding (missing dimensions, etc.)

    [EnumMember(Value = "unoptimized_boot_sequence")]
    UnoptimizedBootSequence = 5 // Many janks during the FCP-LCP window
}
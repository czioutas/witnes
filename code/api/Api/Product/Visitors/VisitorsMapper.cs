using Api.Product.MetricsProcessing.Gold;
using Api.Product.Visitors.Models;
using AutoMapper;
using Libs.Domain;

namespace Api.Product.Visitors;

/// <summary>
/// AutoMapper profile for Metrics mappings
/// </summary>
public class VisitorsMapper : Profile
{
    public VisitorsMapper()
    {
        CreateMap<MetricGoldEntity, PageLoadSummaryModel>()
                    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                    .ForMember(dest => dest.SilverId, opt => opt.MapFrom(src => src.SilverId))
                    .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.EventType))
                    .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? src.GuestId ?? "Anonymous"))
                    .ForMember(dest => dest.UrlPath, opt => opt.MapFrom(src => src.UrlPath))
                    .ForMember(dest => dest.DeviceIcon, opt => opt.MapFrom(src => src.DeviceIcon))
                    .ForMember(dest => dest.BrowserIcon, opt => opt.MapFrom(src => src.BrowserIcon))
                    .ForMember(dest => dest.PageRequestedAtByVisitor, opt => opt.MapFrom(src => src.PageRequestedAtByVisitor))
                    .ForMember(dest => dest.TotalInitialLoadMs, opt => opt.MapFrom(src => src.TotalInitialLoadMs))
                    .ForMember(dest => dest.SettledTimeMs, opt => opt.MapFrom(src => src.SettledTimeMs))

                    // Status Flags
                    .ForMember(dest => dest.Incomplete, opt => opt.MapFrom(src => src.Incomplete))
                    .ForMember(dest => dest.IsBadExperience, opt => opt.MapFrom(src => src.IsBadExperience))
                    // Logic: User left early if the RageQuit symptom is in the list
                    .ForMember(dest => dest.UserLeftEarly, opt => opt.MapFrom(src =>
                        src.ExperienceSymptoms.Contains(ExperienceSymptom.RageQuit)))

                    // Pillar Bools (The Icons)
                    .ForMember(dest => dest.IsBackendIssue, opt => opt.MapFrom(src => src.IsBackendIssue))
                    .ForMember(dest => dest.IsNetworkIssue, opt => opt.MapFrom(src => src.IsNetworkIssue))
                    .ForMember(dest => dest.IsPayloadIssue, opt => opt.MapFrom(src => src.IsPayloadIssue))
                    .ForMember(dest => dest.IsFrontendIssue, opt => opt.MapFrom(src => src.IsFrontendIssue))

                    // Confidence Scores
                    .ForMember(dest => dest.BackendConfidence, opt => opt.MapFrom(src => src.BackendConfidence))
                    .ForMember(dest => dest.NetworkConfidence, opt => opt.MapFrom(src => src.NetworkConfidence))
                    .ForMember(dest => dest.PayloadConfidence, opt => opt.MapFrom(src => src.PayloadConfidence))
                    .ForMember(dest => dest.FrontendConfidence, opt => opt.MapFrom(src => src.FrontendConfidence))

                    // The Raw "Why" (Enums for tooltips/details)
                    .ForMember(dest => dest.BackendReasons, opt => opt.MapFrom(src => src.BackendReasons))
                    .ForMember(dest => dest.NetworkReasons, opt => opt.MapFrom(src => src.NetworkReasons))
                    .ForMember(dest => dest.PayloadReasons, opt => opt.MapFrom(src => src.PayloadReasons))
                    .ForMember(dest => dest.FrontendReasons, opt => opt.MapFrom(src => src.FrontendReasons))
                    .ForMember(dest => dest.ExperienceSymptoms, opt => opt.MapFrom(src => src.ExperienceSymptoms))

                    // Performance Markers
                    .ForMember(dest => dest.ConnectionQuality, opt => opt.MapFrom(src => src.ConnectionQuality))
                    .ForMember(dest => dest.AbsoluteLcpMs, opt => opt.MapFrom(src => src.AbsoluteLcpMs))
                    .ForMember(dest => dest.ClsScore, opt => opt.MapFrom(src => src.ClsScore))
                    .ForMember(dest => dest.TotalJankCount, opt => opt.MapFrom(src => src.TotalJankCount))
                    .ForMember(dest => dest.InteractionDeadZoneMs, opt => opt.MapFrom(src => src.InteractionDeadZoneMs))

                    // Session Stitching
                    .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.SessionId))
                    .ForMember(dest => dest.SessionRef, opt => opt.MapFrom(src => src.SessionRef))

                    // Overall Verdict
                    .ForMember(dest => dest.OverallSentiment, opt => opt.MapFrom(src => src.OverallSentiment))
                    .ForMember(dest => dest.HistoricalComparison, opt => opt.MapFrom(src => src.HistoricalComparison));
    }
}

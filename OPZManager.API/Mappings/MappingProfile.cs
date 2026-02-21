using System.Text.RegularExpressions;
using AutoMapper;
using OPZManager.API.Models;
using OPZManager.API.DTOs.Auth;
using OPZManager.API.DTOs.Equipment;
using OPZManager.API.DTOs.OPZ;
using OPZManager.API.DTOs.Admin;
using OPZManager.API.DTOs.KnowledgeBase;
using OPZManager.API.Services;
using OPZManager.API.DTOs.Public;

namespace OPZManager.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>();

            // Manufacturer mappings
            CreateMap<Manufacturer, ManufacturerDto>();
            CreateMap<CreateManufacturerDto, Manufacturer>();

            // EquipmentType mappings
            CreateMap<EquipmentType, EquipmentTypeDto>();
            CreateMap<CreateEquipmentTypeDto, EquipmentType>();

            // EquipmentModel mappings
            CreateMap<EquipmentModel, EquipmentModelDto>()
                .ForMember(d => d.ManufacturerName, opt => opt.MapFrom(s => s.Manufacturer.Name))
                .ForMember(d => d.TypeName, opt => opt.MapFrom(s => s.Type.Name));
            CreateMap<CreateEquipmentModelDto, EquipmentModel>();

            // OPZDocument mappings
            CreateMap<OPZDocument, OPZDocumentDto>()
                .ForMember(d => d.RequirementsCount, opt => opt.MapFrom(s => s.OPZRequirements.Count))
                .ForMember(d => d.MatchesCount, opt => opt.MapFrom(s => s.EquipmentMatches.Count));
            CreateMap<OPZDocument, OPZDocumentDetailDto>()
                .ForMember(d => d.Requirements, opt => opt.MapFrom(s => s.OPZRequirements))
                .ForMember(d => d.Matches, opt => opt.MapFrom(s => s.EquipmentMatches));

            // OPZRequirement mappings
            CreateMap<OPZRequirement, OPZRequirementDto>()
                .ForMember(d => d.DeviceCategory, opt => opt.MapFrom(s => ParseDeviceCategory(s.RequirementText)));

            // RequirementCompliance mappings
            CreateMap<RequirementCompliance, RequirementComplianceDto>();

            // EquipmentMatch mappings
            CreateMap<EquipmentMatch, EquipmentMatchDto>()
                .ForMember(d => d.ModelName, opt => opt.MapFrom(s => s.EquipmentModel.ModelName))
                .ForMember(d => d.ManufacturerName, opt => opt.MapFrom(s => s.EquipmentModel.Manufacturer.Name))
                .ForMember(d => d.TypeName, opt => opt.MapFrom(s => s.EquipmentModel.Type.Name))
                .ForMember(d => d.RequirementCompliances, opt => opt.MapFrom(s => s.RequirementCompliances));

            // TrainingData mappings
            CreateMap<TrainingData, TrainingDataDto>();
            CreateMap<CreateTrainingDataDto, TrainingData>();

            // KnowledgeDocument mappings
            CreateMap<KnowledgeDocument, KnowledgeDocumentDto>();
            CreateMap<KnowledgeSearchResult, KnowledgeSearchResultDto>();

            // Public OPZ Document mapping
            CreateMap<OPZDocument, PublicOPZDocumentDto>()
                .ForMember(d => d.RequirementsCount, opt => opt.MapFrom(s => s.OPZRequirements.Count))
                .ForMember(d => d.MatchesCount, opt => opt.MapFrom(s => s.EquipmentMatches.Count));
        }

        private static string ParseDeviceCategory(string requirementText)
        {
            var match = Regex.Match(requirementText, @"^\[([^\]]+)\]");
            return match.Success ? match.Groups[1].Value : "Ogólne";
        }
    }
}

namespace Lockdown.Build.Mapping
{
    using System;
    using AutoMapper;
    using Lockdown.Build.Entities;
    using Raw = Lockdown.Build.RawEntities;

    public static class Mapper
    {
        public static IMapper GetMapper()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Raw.PostMetadata, PostMetadata>()
                    .ForMember(dest => dest.Context, opt => opt.Ignore())
                    .ForMember(dest => dest.CanonicalUrl, opt => opt.Ignore())
                    .ForMember(dest => dest.Tags, opt => opt.Ignore())
                    .ForMember(
                        dest => dest.Date,
                        opt => opt.MapFrom(
                            orig => orig.Date.GetValueOrDefault(
                                orig.Date.GetValueOrDefault(DateTime.Now))));

                cfg.CreateMap<Raw.SiteConfiguration, SiteConfiguration>()
                    .ForMember(dest => dest.Context, opt => opt.Ignore());

                cfg.CreateMap<Raw.Link, Link>()
                    .ForMember(dest => dest.Context, opt => opt.Ignore());
            });

            configuration.AssertConfigurationIsValid();

            return configuration.CreateMapper();
        }
    }
}
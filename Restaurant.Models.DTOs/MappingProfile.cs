using AutoMapper;
using Restaurant.Models;

namespace Restaurant.Models.DTOs;

/// <summary>
/// ملف تعريف التحويل التلقائي الخاص بـ AutoMapper
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Category Mappings
        CreateMap<Category, CategoryDto>().ReverseMap();
        CreateMap<CategoryCreateDto, Category>();
        CreateMap<CategoryUpdateDto, Category>();

        // Addon Mappings
        CreateMap<Addon, AddonDto>().ReverseMap();
        CreateMap<AddonCreateDto, Addon>();
        CreateMap<AddonUpdateDto, Addon>();

        // Product Mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));
        CreateMap<ProductCreateDto, Product>();
        CreateMap<ProductUpdateDto, Product>();

        // PrintStation Mappings
        CreateMap<PrintStation, PrintStationDto>().ReverseMap();
        CreateMap<PrintStationCreateDto, PrintStation>();
        CreateMap<PrintStationUpdateDto, PrintStation>();

        // Printer Mappings
        CreateMap<Printer, PrinterDto>()
            .ForMember(dest => dest.PrintStationName, opt => opt.MapFrom(src => src.PrintStation != null ? src.PrintStation.Name : string.Empty));
        CreateMap<PrinterCreateDto, Printer>();
        CreateMap<PrinterUpdateDto, Printer>();

        // PaymentMethod Mappings
        CreateMap<PaymentMethod, PaymentMethodDto>().ReverseMap();
        CreateMap<PaymentMethodCreateDto, PaymentMethod>();
        CreateMap<PaymentMethodUpdateDto, PaymentMethod>();

        // Order Mappings
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : string.Empty));
        CreateMap<OrderCreateDto, Order>();
        CreateMap<OrderUpdateDto, Order>();

        // Cashbox Mappings
        CreateMap<Cashbox, CashboxDto>().ReverseMap();
        CreateMap<CashboxCreateDto, Cashbox>();
        CreateMap<CashboxUpdateDto, Cashbox>();

        // CashDrawerEntry Mappings
        CreateMap<CashDrawerEntry, CashDrawerEntryDto>()
            .ForMember(dest => dest.CashboxName, opt => opt.MapFrom(src => src.Cashbox != null ? src.Cashbox.Name : string.Empty))
            .ForMember(dest => dest.PaymentMethodName, opt => opt.MapFrom(src => src.PaymentMethod != null ? src.PaymentMethod.Name : string.Empty))
            .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderNumber : (int?)null))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : string.Empty));
        CreateMap<CashDrawerEntry, CashDrawerEntrySummaryDto>()
            .ForMember(dest => dest.CashboxName, opt => opt.MapFrom(src => src.Cashbox != null ? src.Cashbox.Name : string.Empty))
            .ForMember(dest => dest.PaymentMethodName, opt => opt.MapFrom(src => src.PaymentMethod != null ? src.PaymentMethod.Name : string.Empty));
        CreateMap<CashDrawerEntryCreateDto, CashDrawerEntry>();
        CreateMap<CashDrawerEntryUpdateDto, CashDrawerEntry>();

        // OrderItem Mappings
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.Product != null ? src.Product.CategoryId : 0));
        CreateMap<OrderItemCreateDto, OrderItem>();
        CreateMap<OrderItemUpdateDto, OrderItem>();

        // CategoryPrintStation Mappings
        CreateMap<CategoryPrintStation, CategoryPrintStationDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.PrintStationName, opt => opt.MapFrom(src => src.PrintStation != null ? src.PrintStation.Name : string.Empty));
        CreateMap<CategoryPrintStationCreateDto, CategoryPrintStation>();

        // ApplicationUser Mappings
        CreateMap<ApplicationUser, UserDto>();

        // Restaurant Mappings
        CreateMap<RestaurantInfo, RestaurantDto>().ReverseMap();
        CreateMap<RestaurantCreateDto, RestaurantInfo>();
        CreateMap<RestaurantUpdateDto, RestaurantInfo>();
    }
}

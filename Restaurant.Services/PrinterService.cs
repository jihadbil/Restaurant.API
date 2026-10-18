using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Models.Enums;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class PrinterService : IPrinterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PrinterService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PrinterDto>> GetAllPrintersAsync()
    {
        var printers = await _unitOfWork.Printers.GetAllAsync(includeProperties: "PrintStation");
        return _mapper.Map<IEnumerable<PrinterDto>>(printers);
    }

    public async Task<PrinterDto?> GetPrinterByIdAsync(int id)
    {
        var printer = await _unitOfWork.Printers.GetFirstOrDefaultAsync(p => p.Id == id, includeProperties: "PrintStation");
        return _mapper.Map<PrinterDto?>(printer);
    }

    public async Task<PrinterDto> CreatePrinterAsync(PrinterCreateDto printerCreateDto)
    {
        var printerName = await ValidateConfigurationAsync(0, printerCreateDto.PrintStationId,
            printerCreateDto.PrinterType, printerCreateDto.PrinterName);
        var printer = _mapper.Map<Printer>(printerCreateDto);
        printer.PrinterName = printerName;
        await _unitOfWork.Printers.AddAsync(printer);
        await _unitOfWork.SaveAsync();
        
        var savedPrinter = await _unitOfWork.Printers.GetFirstOrDefaultAsync(p => p.Id == printer.Id, includeProperties: "PrintStation");
        return _mapper.Map<PrinterDto>(savedPrinter);
    }

    public async Task<bool> UpdatePrinterAsync(PrinterUpdateDto printerUpdateDto)
    {
        var printer = await _unitOfWork.Printers.GetFirstOrDefaultAsync(p => p.Id == printerUpdateDto.Id, tracked: false);
        if (printer == null)
        {
            return false;
        }

        var printerName = await ValidateConfigurationAsync(printer.Id, printerUpdateDto.PrintStationId,
            printerUpdateDto.PrinterType, printerUpdateDto.PrinterName);
        _mapper.Map(printerUpdateDto, printer);
        printer.PrinterName = printerName;
        _unitOfWork.Printers.Update(printer);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    private async Task<string> ValidateConfigurationAsync(int id, int stationId, PrinterType type, string? printerName)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentException("نوع الطابعة غير صالح.");
        var name = printerName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
            throw new ArgumentException("اسم الطابعة الفعلي مطلوب ولا يتجاوز 150 حرفاً.");
        if (!await _unitOfWork.PrintStations.Query().AnyAsync(s => s.Id == stationId))
            throw new ArgumentException("محطة الطباعة المحددة غير موجودة.");
        var normalizedName = name.ToUpperInvariant();
        if (await _unitOfWork.Printers.Query().AnyAsync(p => p.Id != id && p.PrintStationId == stationId
            && p.PrinterType == type && p.PrinterName.Trim().ToUpper() == normalizedName))
            throw new InvalidOperationException("هذه الطابعة مسجلة بالفعل لهذا النوع في المحطة المحددة.");
        return name;
    }

    public async Task<bool> DeletePrinterAsync(int id)
    {
        var printer = await _unitOfWork.Printers.GetFirstOrDefaultAsync(p => p.Id == id);
        if (printer == null)
        {
            return false;
        }

        _unitOfWork.Printers.Remove(printer);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}

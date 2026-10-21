using ChiTieu;
using ChiTieu.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    // Thông báo hiện phía trên thanh điều hướng (vị trí tinh chỉnh bằng CSS để tránh nút +)
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.VisibleStateDuration = 4500;
    config.SnackbarConfiguration.ShowCloseIcon = false;
    config.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddSingleton(new HttpClient());

// Logic thuần (nằm trong ChiTieu.Core, có unit test) - không phụ thuộc gì nên dùng Singleton
builder.Services.AddSingleton<ParserService>();
builder.Services.AddSingleton<NganSachService>();
builder.Services.AddSingleton<MucTieuService>();
builder.Services.AddSingleton<ThongKeService>();

// Dịch vụ gắn với dữ liệu trình duyệt. Dùng Scoped: trong Blazor WebAssembly "scope" chính là
// cả vòng đời của ứng dụng nên hành vi giống Singleton, nhưng được phép dùng ISnackbar (Scoped) của MudBlazor.
builder.Services.AddScoped<DbService>();
builder.Services.AddScoped<DongHo>();
builder.Services.AddScoped<KhoDuLieu>();
builder.Services.AddScoped<GiaoDichService>();
builder.Services.AddScoped<QuanLyNganSach>();
builder.Services.AddScoped<QuanLyMucTieu>();
builder.Services.AddScoped<CaiDatService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<SheetService>();
builder.Services.AddScoped<PhanHoi>();
builder.Services.AddScoped<ThongBao>();
builder.Services.AddScoped<AmThanh>();
builder.Services.AddScoped<SuKienService>();
builder.Services.AddScoped<HoSoService>();

await builder.Build().RunAsync();

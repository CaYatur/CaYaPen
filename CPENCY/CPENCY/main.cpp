#include <windows.h>
#include <windowsx.h>
#include <gdiplus.h>
#include <vector>
#include <commctrl.h>

#pragma comment(lib, "gdiplus.lib")
#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "gdi32.lib")

using namespace Gdiplus;

// Global değişkenler
ULONG_PTR gdiplusToken;
HWND hMainWindow = nullptr;
HWND hToolbar = nullptr;

// Çizim verileri
enum class Tool { PEN, ERASER, MOUSE };
enum class DrawColor { RED, BLUE, GREEN, BLACK };

struct DrawPoint {
    POINT pos;
    DrawColor color;
    int width;
    bool isEraser;
};

std::vector<std::vector<DrawPoint>> strokes;
std::vector<DrawPoint> currentStroke;

// Uygulama durumu
Tool currentTool = Tool::PEN;
DrawColor currentColor = DrawColor::RED;
int penWidth = 5;
bool drawing = false;
bool toolbarVisible = true;

// Buton ID'leri
const int ID_RED = 1001;
const int ID_BLUE = 1002;
const int ID_GREEN = 1003;
const int ID_BLACK = 1004;
const int ID_PEN = 1005;
const int ID_ERASER = 1006;
const int ID_MOUSE = 1007;
const int ID_CLEAR = 1008;
const int ID_HIDE = 1009;
const int ID_EXIT = 1010;

// Yardımcı fonksiyonlar
Color GetGdiplusColor(DrawColor color) {
    switch (color) {
    case DrawColor::RED: return Color(255, 255, 0, 0);
    case DrawColor::BLUE: return Color(255, 0, 0, 255);
    case DrawColor::GREEN: return Color(255, 0, 200, 0);
    case DrawColor::BLACK:
    default: return Color(255, 0, 0, 0);
    }
}

const wchar_t* GetColorName(DrawColor color) {
    switch (color) {
    case DrawColor::RED: return L"🔴 Kırmızı";
    case DrawColor::BLUE: return L"🔵 Mavi";
    case DrawColor::GREEN: return L"🟢 Yeşil";
    case DrawColor::BLACK:
    default: return L"⚫ Siyah";
    }
}

const wchar_t* GetToolName(Tool tool) {
    switch (tool) {
    case Tool::PEN: return L"✏️ Kalem";
    case Tool::ERASER: return L"🧽 Silgi";
    case Tool::MOUSE: return L"🖱️ Fare";
    default: return L"🔧 Araç";
    }
}

void UpdateWindowTitle() {
    wchar_t title[256];
    swprintf(title, sizeof(title) / sizeof(wchar_t),
        L"🎨 CaYatur Ekran Kalemi - %s | %s | %dpx - 2025-05-31 09:56:35",
        GetToolName(currentTool), GetColorName(currentColor), penWidth);
    SetWindowText(hMainWindow, title);
}

void CreateToolbar() {
    if (!hMainWindow) return;

    // Toolbar oluştur
    hToolbar = CreateWindow(L"STATIC", L"",
        WS_CHILD | WS_VISIBLE | WS_BORDER,
        10, 10, 700, 70,
        hMainWindow, nullptr, GetModuleHandle(nullptr), nullptr);

    if (!hToolbar) return;

    int x = 10, y = 15;
    int buttonWidth = 60, buttonHeight = 40;
    int spacing = 70;

    // Başlık
    CreateWindow(L"STATIC", L"🎨 CaYatur Ekran Kalemi - 2025-05-31 09:56:35 UTC",
        WS_CHILD | WS_VISIBLE | SS_LEFT,
        x, y - 10, 400, 15, hToolbar, nullptr, GetModuleHandle(nullptr), nullptr);

    // Renk butonları
    CreateWindow(L"BUTTON", L"🔴", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_RED, GetModuleHandle(nullptr), nullptr);
    x += spacing;

    CreateWindow(L"BUTTON", L"🔵", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_BLUE, GetModuleHandle(nullptr), nullptr);
    x += spacing;

    CreateWindow(L"BUTTON", L"🟢", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_GREEN, GetModuleHandle(nullptr), nullptr);
    x += spacing;

    CreateWindow(L"BUTTON", L"⚫", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_BLACK, GetModuleHandle(nullptr), nullptr);
    x += spacing + 20;

    // Araç butonları
    CreateWindow(L"BUTTON", L"✏️", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_PEN, GetModuleHandle(nullptr), nullptr);
    x += spacing;

    CreateWindow(L"BUTTON", L"🧽", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_ERASER, GetModuleHandle(nullptr), nullptr);
    x += spacing;

    CreateWindow(L"BUTTON", L"🖱️", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_MOUSE, GetModuleHandle(nullptr), nullptr);
    x += spacing + 20;

    // Kontrol butonları
    CreateWindow(L"BUTTON", L"🗑️", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_CLEAR, GetModuleHandle(nullptr), nullptr);
    x += spacing;

    CreateWindow(L"BUTTON", L"❌", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
        x, y, buttonWidth, buttonHeight, hToolbar, (HMENU)ID_EXIT, GetModuleHandle(nullptr), nullptr);
}

void OnPaint(HDC hdc) {
    RECT clientRect;
    GetClientRect(hMainWindow, &clientRect);

    // Çizim alanını ayarla (toolbar altından başla)
    if (toolbarVisible) {
        clientRect.top = 90;
    }

    // Arka planı beyaz yap
    FillRect(hdc, &clientRect, (HBRUSH)GetStockObject(WHITE_BRUSH));

    Graphics graphics(hdc);
    graphics.SetSmoothingMode(SmoothingModeAntiAlias);

    // Çizilmiş tüm çizgileri göster
    for (const auto& stroke : strokes) {
        if (stroke.empty()) continue;

        for (size_t i = 0; i < stroke.size(); ++i) {
            const DrawPoint& point = stroke[i];

            Color color = point.isEraser ? Color(255, 255, 255, 255) : GetGdiplusColor(point.color);
            Pen pen(color, static_cast<REAL>(point.width));
            pen.SetLineCap(LineCapRound, LineCapRound, DashCapRound);

            if (i == 0) {
                SolidBrush brush(color);
                REAL radius = static_cast<REAL>(point.width) / 2.0f;
                graphics.FillEllipse(&brush,
                    static_cast<REAL>(point.pos.x) - radius,
                    static_cast<REAL>(point.pos.y) - radius,
                    static_cast<REAL>(point.width),
                    static_cast<REAL>(point.width));
            }
            else {
                const DrawPoint& prevPoint = stroke[i - 1];
                graphics.DrawLine(&pen,
                    static_cast<REAL>(prevPoint.pos.x), static_cast<REAL>(prevPoint.pos.y),
                    static_cast<REAL>(point.pos.x), static_cast<REAL>(point.pos.y));
            }
        }
    }

    // Şu anki çizgiyi göster
    if (!currentStroke.empty()) {
        for (size_t i = 0; i < currentStroke.size(); ++i) {
            const DrawPoint& point = currentStroke[i];

            Color color = point.isEraser ? Color(255, 255, 255, 255) : GetGdiplusColor(point.color);
            Pen pen(color, static_cast<REAL>(point.width));
            pen.SetLineCap(LineCapRound, LineCapRound, DashCapRound);

            if (i == 0) {
                SolidBrush brush(color);
                REAL radius = static_cast<REAL>(point.width) / 2.0f;
                graphics.FillEllipse(&brush,
                    static_cast<REAL>(point.pos.x) - radius,
                    static_cast<REAL>(point.pos.y) - radius,
                    static_cast<REAL>(point.width),
                    static_cast<REAL>(point.width));
            }
            else {
                const DrawPoint& prevPoint = currentStroke[i - 1];
                graphics.DrawLine(&pen,
                    static_cast<REAL>(prevPoint.pos.x), static_cast<REAL>(prevPoint.pos.y),
                    static_cast<REAL>(point.pos.x), static_cast<REAL>(point.pos.y));
            }
        }
    }
}

void OnMouseDown(int x, int y) {
    if (currentTool == Tool::MOUSE) return;
    if (y < 90 && toolbarVisible) return; // Toolbar alanında çizim yapma

    drawing = true;
    SetCapture(hMainWindow);
    currentStroke.clear();

    DrawPoint point;
    point.pos = { x, y };
    point.color = currentColor;
    point.width = penWidth;
    point.isEraser = (currentTool == Tool::ERASER);

    currentStroke.push_back(point);
    InvalidateRect(hMainWindow, nullptr, FALSE);
}

void OnMouseMove(int x, int y) {
    if (!drawing || currentTool == Tool::MOUSE) return;

    DrawPoint point;
    point.pos = { x, y };
    point.color = currentColor;
    point.width = penWidth;
    point.isEraser = (currentTool == Tool::ERASER);

    currentStroke.push_back(point);
    InvalidateRect(hMainWindow, nullptr, FALSE);
}

void OnMouseUp(int x, int y) {
    if (!drawing || currentTool == Tool::MOUSE) return;

    drawing = false;
    ReleaseCapture();

    if (!currentStroke.empty()) {
        strokes.push_back(currentStroke);
        currentStroke.clear();
    }
}

void OnCommand(int id) {
    switch (id) {
    case ID_RED:
        currentColor = DrawColor::RED;
        break;
    case ID_BLUE:
        currentColor = DrawColor::BLUE;
        break;
    case ID_GREEN:
        currentColor = DrawColor::GREEN;
        break;
    case ID_BLACK:
        currentColor = DrawColor::BLACK;
        break;
    case ID_PEN:
        currentTool = Tool::PEN;
        SetCursor(LoadCursor(nullptr, IDC_CROSS));
        break;
    case ID_ERASER:
        currentTool = Tool::ERASER;
        SetCursor(LoadCursor(nullptr, IDC_CROSS));
        break;
    case ID_MOUSE:
        currentTool = Tool::MOUSE;
        SetCursor(LoadCursor(nullptr, IDC_ARROW));
        break;
    case ID_CLEAR: {
        int result = MessageBox(hMainWindow,
            L"🗑️ Tüm çizimleri silmek istediğinizden emin misiniz?\n\n2025-05-31 09:56:35 UTC",
            L"⚠️ CaYatur - Dikkat!",
            MB_YESNO | MB_ICONQUESTION);

        if (result == IDYES) {
            strokes.clear();
            currentStroke.clear();
            InvalidateRect(hMainWindow, nullptr, TRUE);
            MessageBox(hMainWindow, L"✅ Temizlendi!", L"🎨 CaYatur", MB_OK | MB_ICONINFORMATION);
        }
        break;
    }
    case ID_HIDE:
        toolbarVisible = !toolbarVisible;
        ShowWindow(hToolbar, toolbarVisible ? SW_SHOW : SW_HIDE);
        InvalidateRect(hMainWindow, nullptr, TRUE);
        break;
    case ID_EXIT:
        if (MessageBox(hMainWindow, L"🚪 Çıkmak istediğinizden emin misiniz?",
            L"❓ CaYatur Çıkış", MB_YESNO | MB_ICONQUESTION) == IDYES) {
            PostQuitMessage(0);
        }
        break;
    }
    UpdateWindowTitle();
}

LRESULT CALLBACK WindowProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam) {
    switch (message) {
    case WM_CREATE:
        CreateToolbar();
        UpdateWindowTitle();

        // Hoşgeldin mesajı
        MessageBox(hWnd,
            L"🎉 CaYatur Ekran Kalemi başarıyla başlatıldı!\n\n"
            L"📅 Tarih: 2025-05-31 09:56:35 UTC\n"
            L"👤 Kullanıcı: CaYatur\n"
            L"💻 Sistem: Windows GUI\n\n"
            L"🎨 Özellikler:\n"
            L"• 4 Renk (🔴🔵🟢⚫)\n"
            L"• Kalem, Silgi, Fare araçları\n"
            L"• Ayarlanabilir kalınlık\n\n"
            L"🎯 Kontroller (Sadece Butonlar):\n"
            L"• Renk butonları: 🔴🔵🟢⚫\n"
            L"• Araç butonları: ✏️🧽🖱️\n"
            L"• Kontrol butonları: 🗑️❌\n\n"
            L"✨ İyi kullanımlar! 🚀",
            L"🎨 CaYatur Ekran Kalemi - Hoşgeldiniz!",
            MB_OK | MB_ICONINFORMATION);
        return 0;

    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hWnd, &ps);
        OnPaint(hdc);
        EndPaint(hWnd, &ps);
        return 0;
    }

    case WM_LBUTTONDOWN:
        OnMouseDown(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
        return 0;

    case WM_MOUSEMOVE:
        if (wParam & MK_LBUTTON) {
            OnMouseMove(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
        }
        return 0;

    case WM_LBUTTONUP:
        OnMouseUp(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
        return 0;

    case WM_COMMAND:
        OnCommand(LOWORD(wParam));
        return 0;

    case WM_MOUSEWHEEL: {
        // Mouse wheel ile kalınlık ayarı
        int delta = GET_WHEEL_DELTA_WPARAM(wParam);
        if (delta > 0 && penWidth < 20) {
            penWidth++;
            UpdateWindowTitle();
        }
        else if (delta < 0 && penWidth > 1) {
            penWidth--;
            UpdateWindowTitle();
        }
        return 0;
    }

    case WM_SIZE:
        if (hToolbar) {
            // Toolbar'ı yeniden boyutlandır
            RECT clientRect;
            GetClientRect(hWnd, &clientRect);
            SetWindowPos(hToolbar, nullptr, 10, 10,
                clientRect.right - 20, 70, SWP_NOZORDER);
        }
        return 0;

    case WM_CLOSE:
        OnCommand(ID_EXIT);
        return 0;

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }

    return DefWindowProc(hWnd, message, wParam, lParam);
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    // GDI+ başlat
    GdiplusStartupInput gdiplusStartupInput;
    Status status = GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);
    if (status != Ok) {
        MessageBox(nullptr, L"❌ GDI+ başlatılamadı!", L"CaYatur - Hata", MB_OK | MB_ICONERROR);
        return -1;
    }

    // Common Controls başlat
    INITCOMMONCONTROLSEX icex;
    icex.dwSize = sizeof(INITCOMMONCONTROLSEX);
    icex.dwICC = ICC_BAR_CLASSES;
    InitCommonControlsEx(&icex);

    // Window class kaydet
    WNDCLASSEX wcex = {};
    wcex.cbSize = sizeof(WNDCLASSEX);
    wcex.style = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc = WindowProc;
    wcex.hInstance = hInstance;
    wcex.hIcon = LoadIcon(nullptr, IDI_APPLICATION);
    wcex.hCursor = LoadCursor(nullptr, IDC_CROSS);
    wcex.hbrBackground = (HBRUSH)GetStockObject(WHITE_BRUSH);
    wcex.lpszClassName = L"CaYaturScreenPen";
    wcex.hIconSm = LoadIcon(nullptr, IDI_APPLICATION);

    if (!RegisterClassEx(&wcex)) {
        MessageBox(nullptr, L"❌ Window class kaydedilemedi!", L"CaYatur - Hata", MB_OK | MB_ICONERROR);
        GdiplusShutdown(gdiplusToken);
        return -1;
    }

    // Ekran boyutlarını al
    int screenWidth = GetSystemMetrics(SM_CXSCREEN);
    int screenHeight = GetSystemMetrics(SM_CYSCREEN);

    // Ana pencere oluştur - Tam ekran
    hMainWindow = CreateWindowEx(
        WS_EX_TOPMOST,                          // Her zaman üstte
        L"CaYaturScreenPen",
        L"🎨 CaYatur Ekran Kalemi - 2025-05-31 09:56:35",
        WS_OVERLAPPEDWINDOW,                    // Normal pencere
        0, 0,                                   // Sol üst köşe
        screenWidth, screenHeight,              // Tam ekran boyut
        nullptr, nullptr, hInstance, nullptr
    );

    if (!hMainWindow) {
        DWORD error = GetLastError();
        wchar_t errorMsg[512];
        swprintf(errorMsg, sizeof(errorMsg) / sizeof(wchar_t),
            L"❌ Ana pencere oluşturulamadı!\n\n"
            L"🔍 Hata Kodu: %lu\n"
            L"📅 Tarih: 2025-05-31 09:56:35 UTC\n"
            L"👤 Kullanıcı: CaYatur\n\n"
            L"💡 Çözümler:\n"
            L"1. Yönetici olarak çalıştırın\n"
            L"2. Antivirüs programını kontrol edin\n"
            L"3. Sistemi yeniden başlatın",
            error);

        MessageBox(nullptr, errorMsg, L"🚨 CaYatur - Kritik Hata", MB_OK | MB_ICONERROR);
        GdiplusShutdown(gdiplusToken);
        return -1;
    }

    // Pencereyi göster
    ShowWindow(hMainWindow, SW_MAXIMIZE);  // Tam ekran göster
    UpdateWindow(hMainWindow);

    // Ana mesaj döngüsü
    MSG msg;
    while (GetMessage(&msg, nullptr, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    // Temizlik
    GdiplusShutdown(gdiplusToken);
    return (int)msg.wParam;
}

int main() {
    return WinMain(GetModuleHandle(nullptr), nullptr, GetCommandLineA(), SW_SHOW);
}
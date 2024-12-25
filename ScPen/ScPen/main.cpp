#include <windows.h>

// Global Variables
COLORREF currentColor = RGB(0, 0, 0); // Default: Black
bool isErasing = false;
HBITMAP hBitmap = NULL; // Bitmap to capture screen
HWND hwndToolbar = NULL; // Toolbar window handle

// Window Procedure
LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
    static POINT prevPoint;
    static HDC screenDC, memDC;
    static HBRUSH hBrush;

    switch (uMsg) {
    case WM_CREATE: {
        // Capture screen and create memory DC
        screenDC = GetDC(NULL); // Get the entire screen DC
        memDC = CreateCompatibleDC(screenDC);
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);
        hBitmap = CreateCompatibleBitmap(screenDC, screenWidth, screenHeight);
        SelectObject(memDC, hBitmap);
        BitBlt(memDC, 0, 0, screenWidth, screenHeight, screenDC, 0, 0, SRCCOPY); // Capture the screen
        return 0;
    }

    case WM_LBUTTONDOWN: // Start Drawing
        prevPoint.x = LOWORD(lParam);
        prevPoint.y = HIWORD(lParam);
        return 0;

    case WM_MOUSEMOVE: // Drawing
        if (wParam & MK_LBUTTON) {
            POINT currentPoint;
            currentPoint.x = LOWORD(lParam);
            currentPoint.y = HIWORD(lParam);

            HPEN hPen;
            if (isErasing) {
                // For erasing, set a large pen and transparent behavior (simply use background color)
                hPen = CreatePen(PS_SOLID, 10, RGB(255, 255, 255)); // White erase
            }
            else {
                // Regular drawing with current color
                hPen = CreatePen(PS_SOLID, 2, currentColor);
            }
            SelectObject(memDC, hPen);

            MoveToEx(memDC, prevPoint.x, prevPoint.y, NULL);
            LineTo(memDC, currentPoint.x, currentPoint.y);
            prevPoint = currentPoint;

            DeleteObject(hPen); // Clean up

            // Update the window with the new drawing
            HDC hdc = GetDC(hwnd);
            BitBlt(hdc, 0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN), memDC, 0, 0, SRCCOPY);
            ReleaseDC(hwnd, hdc);
        }
        return 0;

    case WM_RBUTTONDOWN: // Toggle Erasing Mode
        isErasing = !isErasing;
        return 0;

    case WM_COMMAND: // Handle Menu Commands
        switch (LOWORD(wParam)) {
        case 1: currentColor = RGB(0, 0, 0); break; // Black
        case 2: currentColor = RGB(255, 0, 0); break; // Red
        case 3: currentColor = RGB(0, 0, 255); break; // Blue
        case 4: currentColor = RGB(0, 255, 0); break; // Green
        }
        return 0;

    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hwnd, &ps);
        BitBlt(hdc, 0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN), memDC, 0, 0, SRCCOPY);
        EndPaint(hwnd, &ps);
        return 0;
    }

    case WM_DESTROY: // Quit Application
        DeleteDC(memDC);
        ReleaseDC(NULL, screenDC);
        DeleteObject(hBitmap);
        PostQuitMessage(0);
        return 0;

    case WM_NCDESTROY:
        if (hwndToolbar) {
            DestroyWindow(hwndToolbar); // Destroy toolbar on window close
        }
        return 0;
    }

    return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

// Toolbar Window Procedure (Popup)
LRESULT CALLBACK ToolbarProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
    switch (uMsg) {
    case WM_LBUTTONDOWN: {
        // If the user clicks on the toolbar, allow dragging the window
        SetCapture(hwnd);
        return 0;
    }

    case WM_MOUSEMOVE: {
        if (wParam & MK_LBUTTON) {
            POINT pt;
            GetCursorPos(&pt);
            SetWindowPos(hwnd, HWND_TOPMOST, pt.x, pt.y, 0, 0, SWP_NOSIZE);
        }
        return 0;
    }

    case WM_LBUTTONUP: {
        ReleaseCapture();
        return 0;
    }

    case WM_COMMAND: // Handle Toolbar Menu Commands
        switch (LOWORD(wParam)) {
        case 1: currentColor = RGB(0, 0, 0); break; // Black
        case 2: currentColor = RGB(255, 0, 0); break; // Red
        case 3: currentColor = RGB(0, 0, 255); break; // Blue
        case 4: currentColor = RGB(0, 255, 0); break; // Green
        }
        return 0;

    case WM_DESTROY:
        hwndToolbar = NULL;
        return 0;
    }

    return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

// Create Menu
void AddMenu(HWND hwnd) {
    HMENU hMenu = CreateMenu();
    HMENU hColorMenu = CreateMenu();

    AppendMenu(hColorMenu, MF_STRING, 1, L"Siyah");
    AppendMenu(hColorMenu, MF_STRING, 2, L"Kýrmýzý");
    AppendMenu(hColorMenu, MF_STRING, 3, L"Mavi");
    AppendMenu(hColorMenu, MF_STRING, 4, L"Yeþil");

    AppendMenu(hMenu, MF_POPUP, (UINT_PTR)hColorMenu, L"Renkler");
    SetMenu(hwnd, hMenu);
}

// Create Toolbar as a Popup
void CreateToolbar(HINSTANCE hInstance) {
    const wchar_t CLASS_NAME[] = L"ToolbarWindow";
    WNDCLASS wc = {};
    wc.lpfnWndProc = ToolbarProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = CLASS_NAME;

    RegisterClass(&wc);

    hwndToolbar = CreateWindowEx(
        WS_EX_LAYERED | WS_EX_TOPMOST, // Transparent and topmost window
        CLASS_NAME,
        L"ToolBar",
        WS_POPUP | WS_BORDER,
        100, 100, 300, 60, // Position and size of toolbar
        NULL, NULL, hInstance, NULL
    );

    SetLayeredWindowAttributes(hwndToolbar, 0, 255, LWA_ALPHA); // Make window transparent
    ShowWindow(hwndToolbar, SW_SHOW);
}

// WinMain
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    const wchar_t CLASS_NAME[] = L"Screen Pen";

    WNDCLASS wc = {};
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = CLASS_NAME;
    wc.hbrBackground = NULL; // Transparent background

    RegisterClass(&wc);

    HWND hwnd = CreateWindowEx(
        WS_EX_LAYERED | WS_EX_TOPMOST, // Transparent and topmost window
        CLASS_NAME,
        L"Ekran Kalemi",
        WS_POPUP,
        0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN),
        NULL,
        NULL,
        hInstance,
        NULL
    );

    if (hwnd == NULL) return 0;

    SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA); // Make window transparent
    AddMenu(hwnd);
    ShowWindow(hwnd, nCmdShow);

    // Create the toolbar window (popup)
    CreateToolbar(hInstance);

    MSG msg = {};
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return 0;
}

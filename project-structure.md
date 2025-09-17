## client folder
### Program.cs
```cs
using AIExamIDE.Components;
using AIExamIDE.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Register custom services
builder.Services.AddScoped<ExamState>();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:3000");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```
### Components folder
#### _Imports.razor
```
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using AIExamIDE
@using AIExamIDE.Components
@using AIExamIDE.Components.Layout
@using AIExamIDE.Components.Pages
@using AIExamIDE.Components.Shared
@using AIExamIDE.Services
@using AIExamIDE.Models
@using MudBlazor
```
#### App.razor
```
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="css/app.css" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet />
</head>

<body>
    <Routes />

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>
    <script src="js/fullscreen.js"></script>
    <script src="_framework/blazor.web.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    
    <!-- Ace Editor Scripts -->
    <script src="https://cdnjs.cloudflare.com/ajax/libs/ace/1.4.12/ace.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/ace/1.4.12/mode-java.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/ace/1.4.12/theme-monokai.js"></script>
    
    <script>
        window.aceEditor = null;

        window.createAceEditor = function (elementId, content, language, readOnly) {
            return window.updateAceEditor(elementId, content, language, readOnly);
        };

        window.updateAceEditor = function (elementId, content, language, readOnly) {
            return new Promise((resolve, reject) => {
                try {
                    const element = document.getElementById(elementId);
                    if (!element) {
                        setTimeout(() => window.updateAceEditor(elementId, content, language, readOnly), 50);
                        return;
                    }

                    // Create editor if it doesn't exist
                    if (!window.aceEditor) {
                        element.innerHTML = '';
                        window.aceEditor = ace.edit(elementId);
                        
                        window.aceEditor.setOptions({
                            fontSize: "14px",
                            showLineNumbers: true,
                            showGutter: true,
                            highlightActiveLine: true,
                            wrap: true,
                            enableBasicAutocompletion: true,
                            enableLiveAutocompletion: true,
                            printMargin: false
                        });
                    }

                    // Update editor content and settings
                    window.aceEditor.setTheme("ace/theme/eclipse");
                    window.aceEditor.session.setMode("ace/mode/" + language);
                    window.aceEditor.setValue(content || '', -1);
                    window.aceEditor.setReadOnly(readOnly || false);
                    
                    // Force resize
                    setTimeout(() => {
                        window.aceEditor.resize();
                    }, 100);
                    
                    resolve();
                    
                } catch (error) {
                    console.error("❌ Error updating Ace editor:", error);
                    reject(error);
                }
            });
        };

        window.getAceEditorContent = function (elementId) {
            try {
                if (window.aceEditor) {
                    return window.aceEditor.getValue();
                }
                return "";
            } catch (error) {
                console.error("❌ Error getting Ace editor content:", error);
                return "";
            }
        };

        window.setAceEditorContent = function (elementId, content) {
            try {
                if (window.aceEditor) {
                    window.aceEditor.setValue(content || '', -1);
                }
            } catch (error) {
                console.error("❌ Error setting Ace editor content:", error);
            }
        };

        window.destroyAceEditor = function (elementId) {
            try {
                if (window.aceEditor) {
                    window.aceEditor.destroy();
                    window.aceEditor = null;
                }
            } catch (error) {
                console.error("❌ Error destroying Ace editor:", error);
            }
        };

        // Resize editor when window is resized
        window.addEventListener('resize', function() {
            if (window.aceEditor && window.aceEditor.resize) {
                window.aceEditor.resize();
            }
        });
    </script>
</body>

</html>
```
#### Routes.razor
```
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```
#### Dialogs folder
##### AddFileDialog.razor
```
@using MudBlazor

<MudDialog>
    <DialogContent>
        <MudForm @ref="_form">
            <MudStack Spacing="2">
                <MudSelect T="string" Label="Directory" Dense="true" @bind-Value="_dir">
                    <MudSelectItem Value="@string.Empty">Root</MudSelectItem>
                    @foreach (var dir in Directories.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d))
                    {
                        <MudSelectItem Value="@dir">@dir</MudSelectItem>
                    }
                </MudSelect>

                <MudTextField @bind-Value="_name"
                            Label="File name"
                            Dense="true"
                            Immediate="true"
                            Error="@(!string.IsNullOrEmpty(_nameError))"
                            ErrorText="@_nameError" />

                <MudTextField @bind-Value="_content"
                            Label="Initial content (optional)"
                            Lines="6" />

                <MudStack Row Justify="Justify.FlexEnd" Spacing="1">
                    <MudButton Variant="Variant.Text" OnClick="@Cancel">Cancel</MudButton>
                    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="@Submit">Create</MudButton>
                </MudStack>
            </MudStack>
        </MudForm>
    </DialogContent>
</MudDialog>

@code {
    [CascadingParameter] public MudBlazor.IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public string Title { get; set; } = "Add a new file";
    [Parameter] public List<string> Directories { get; set; } = new();
    [Parameter] public string DefaultDirectory { get; set; } = "";

    private MudForm? _form;
    private string _dir = "";
    private string _name = "";
    private string _content = "";
    private string? _nameError;

    protected override void OnInitialized()
    {
        // dacă nu ai folder implicit, încearcă primul din listă
        _dir = string.IsNullOrWhiteSpace(DefaultDirectory)
            ? Directories.FirstOrDefault() ?? ""
            : DefaultDirectory;
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task Submit()
    {
        await _form!.Validate();
        _nameError = string.IsNullOrWhiteSpace(_name) ? "File name is required" : null;
        if (!string.IsNullOrEmpty(_nameError)) return;

        MudDialog.Close(DialogResult.Ok(new AddFileResult(_dir, _name, _content)));
    }

    public record AddFileResult(string Directory, string FileName, string? InitialContent)
    {
        public string FullPath => string.IsNullOrWhiteSpace(Directory) ? FileName : $"{Directory}/{FileName}";
    }
}
```
##### DeleteFileDialog.razor
```
@using MudBlazor

<MudDialog>
    <DialogContent>
        <MudStack Spacing="2">
            <MudAlert Severity="Severity.Warning" Dense="true">
                Are you sure you want to delete <strong>@Path</strong>?
            </MudAlert>

            <MudStack Row Justify="Justify.FlexEnd" Spacing="1">
                <MudButton Variant="Variant.Text" OnClick="@Cancel">Cancel</MudButton>
                <MudButton Variant="Variant.Filled" Color="Color.Error" OnClick="@Confirm">Delete</MudButton>
            </MudStack>
        </MudStack>
    </DialogContent>
</MudDialog>

@code {
    [CascadingParameter] public MudBlazor.IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public string Path { get; set; } = string.Empty;

    private void Confirm() => MudDialog.Close(DialogResult.Ok(new DeleteFileResult(Path, true)));
    private void Cancel() => MudDialog.Cancel();

    public record DeleteFileResult(string Path, bool Confirmed);
}
```
##### RenameFileDialog.razor
```
@using MudBlazor

<MudDialog>
    <DialogContent>
        <MudForm @ref="_form">
            <MudStack Spacing="2">
                <MudTextField @bind-Value="_newPath"
                              Label="New path"
                              Dense="true"
                              Immediate="true"
                              Error="@(!string.IsNullOrEmpty(_err))"
                              ErrorText="@_err" />

                <MudStack Row Justify="Justify.FlexEnd" Spacing="1">
                    <MudButton Variant="Variant.Text" OnClick="@Cancel">Cancel</MudButton>
                    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="@Submit">Rename</MudButton>
                </MudStack>
            </MudStack>
        </MudForm>
    </DialogContent>
</MudDialog>

@code {
    [CascadingParameter] public MudBlazor.IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public string OriginalPath { get; set; } = string.Empty;

    private MudForm? _form;
    private string _newPath = string.Empty;
    private string? _err;

    protected override void OnInitialized() => _newPath = OriginalPath;

    private void Cancel() => MudDialog.Cancel();

    private async Task Submit()
    {
        await _form!.Validate();
        _err = string.IsNullOrWhiteSpace(_newPath) ? "New path required" : null;
        if (!string.IsNullOrEmpty(_err)) return;

        MudDialog.Close(DialogResult.Ok(new RenameFileResult(OriginalPath, _newPath)));
    }

    public record RenameFileResult(string OriginalPath, string NewPath);
}
```
#### Shared folder
##### ConsoleOutput.razor
```
@implements IDisposable
@inject ExamState ExamState

<div class="h-full d-flex flex-column">
    <MudPaper Class="pa-2 border-b border-gray-600" Elevation="0" Square="true" Style="background-color: #2d2d2d;">
        <MudText Typo="Typo.subtitle2" Style="color: #ffffff;">Console Output</MudText>
    </MudPaper>
    <div class="flex-grow overflow-auto pa-2" style="background-color: #1a1a1a; color: #ffffff; font-family: 'Consolas', 'Monaco', 'Courier New', monospace;">
        <pre style="margin: 0; white-space: pre-wrap;">@ExamState.ConsoleOutput</pre>
    </div>
</div>

@code {
    private bool _disposed = false;

    protected override void OnInitialized()
    {
        ExamState.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (ExamState != null)
            {
                ExamState.OnChange -= StateHasChanged;
            }
        }
    }
}
```
##### EditorTabs.razor
```
@implements IDisposable
@inject ExamState ExamState
@inject IJSRuntime JSRuntime

<div style="height: 100%; display: flex; flex-direction: column;">
    <!-- Tab Headers -->
    @if (ExamState.OpenFiles.Any())
    {
        <MudTabs Elevation="2" 
                Rounded="true" 
                Centered="false"
                Color="Color.Primary" 
                ActivePanelIndex="GetActiveTabIndex()"
                ActivePanelIndexChanged="OnActiveTabChanged"
                Style="flex-shrink: 0;">
            @foreach (var file in ExamState.OpenFiles)
            {
                <MudTabPanel Text="@file.Name" 
                           Icon="@GetFileIcon(file)">
                    <ChildContent>
                        <!-- Content handled outside tabs -->
                    </ChildContent>
                </MudTabPanel>
            }
        </MudTabs>
    }

    <!-- Editor Content Area -->
    @if (ExamState.ActiveFile != null)
    {
        <!-- Editor Toolbar -->
        <MudPaper Class="pa-2" Elevation="0" Square="true" Style="border-bottom: 1px solid #e0e0e0; flex-shrink: 0;">
            <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                <MudStack Row AlignItems="AlignItems.Center">
                    <MudIcon Icon="@GetFileIcon(ExamState.ActiveFile)" />
                    <MudText Typo="Typo.body2">
                        Editing: @ExamState.ActiveFile.Name
                    </MudText>
                </MudStack>
                <MudStack Row Spacing="2" AlignItems="AlignItems.Center">
                    <MudChip T="string" Size="Size.Small" Variant="Variant.Text">
                        @GetLanguage(ExamState.ActiveFile)
                    </MudChip>
                    <MudChip T="string" Size="Size.Small" Variant="Variant.Text">
                        Lines: @GetLineCount(ExamState.ActiveFile.Content)
                    </MudChip>
                    <MudButton StartIcon="@Icons.Material.Filled.Save" 
                             Size="Size.Small" 
                             Variant="Variant.Filled" 
                             Color="Color.Primary"
                             OnClick="SaveCurrentContent">
                        Save
                    </MudButton>
                    <MudButton StartIcon="@Icons.Material.Filled.FormatAlignLeft" 
                             Size="Size.Small" 
                             Variant="Variant.Filled" 
                             Color="Color.Secondary"
                             OnClick="FormatCode">
                        Format
                    </MudButton>
                    <MudIconButton Icon="@Icons.Material.Filled.Close" 
                                 Size="Size.Small" 
                                 OnClick="() => CloseFile(ExamState.ActiveFile)" />
                </MudStack>
            </MudStack>
        </MudPaper>
        
        <!-- Single Ace Editor Container -->
        <div style="flex-grow: 1; position: relative; min-height: 400px;">
            <div id="ace-editor-main" 
                 style="position: absolute; top: 0; left: 0; right: 0; bottom: 0;">
            </div>
        </div>
        
        <!-- Status Bar -->
        <MudPaper Class="pa-2" Elevation="0" Square="true" Style="border-top: 1px solid #e0e0e0; flex-shrink: 0;">
            <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                <MudText Typo="Typo.caption">
                    Characters: @ExamState.ActiveFile.Content.Length
                </MudText>
                <MudStack Row Spacing="2" AlignItems="AlignItems.Center">
                    <MudText Typo="Typo.caption">@GetLanguage(ExamState.ActiveFile)</MudText>
                    <MudText Typo="Typo.caption">Ace Editor</MudText>
                    <MudIcon Icon="@Icons.Material.Filled.CheckCircle" 
                           Size="Size.Small" 
                           Color="Color.Success" />
                </MudStack>
            </MudStack>
        </MudPaper>
    }
    else
    {
        <div style="flex-grow: 1; display: flex; align-items: center; justify-content: center;">
            <MudStack AlignItems="AlignItems.Center">
                <MudIcon Icon="@Icons.Material.Filled.Description" Size="Size.Large" Color="Color.Default" />
                <MudText Typo="Typo.h6" Color="Color.Default">Select a file to edit</MudText>
                <MudText Typo="Typo.body2" Color="Color.Default">Choose a file from the Solution Explorer</MudText>
            </MudStack>
        </div>
    }
</div>

@code {
    private Dictionary<string, string> fileContents = new();
    private ExamFile? lastActiveFile = null;
    private bool _disposed = false;
    private bool _isUpdatingEditor = false;

    protected override void OnInitialized()
    {
        ExamState.OnChange += OnStateChanged;
    }

    private void OnStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || _isUpdatingEditor) return;

        // Only update editor if active file changed
        if (ExamState.ActiveFile != lastActiveFile)
        {
            await UpdateEditor();
            lastActiveFile = ExamState.ActiveFile;
        }
    }

    private async Task UpdateEditor()
    {
        if (_disposed || ExamState.ActiveFile == null || _isUpdatingEditor) return;

        _isUpdatingEditor = true;

        try
        {
            // Save content from previous file if it exists
            if (lastActiveFile != null)
            {
                await SaveEditorContent(lastActiveFile);
            }

            // Load content for current file
            var currentFile = ExamState.ActiveFile;
            var language = GetAceLanguage(currentFile);

            // Get stored content or use file content
            var content = fileContents.ContainsKey(currentFile.Path) 
                ? fileContents[currentFile.Path] 
                : currentFile.Content;

            Console.WriteLine($"🔄 Switching to {currentFile.Name}");

            // Update the single editor with new content
            await JSRuntime.InvokeVoidAsync("window.updateAceEditor", 
                "ace-editor-main", 
                content, 
                language, 
                ExamState.IsSubmitted);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating editor: {ex.Message}");
        }
        finally
        {
            _isUpdatingEditor = false;
        }
    }

    private async Task SaveEditorContent(ExamFile file)
    {
        if (_disposed) return;

        try
        {
            var content = await JSRuntime.InvokeAsync<string>("window.getAceEditorContent", "ace-editor-main");
            if (content != null)
            {
                file.Content = content;
                fileContents[file.Path] = content;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving content for {file.Name}: {ex.Message}");
        }
    }

    private int GetActiveTabIndex()
    {
        if (ExamState.ActiveFile == null) return 0;
        return ExamState.OpenFiles.IndexOf(ExamState.ActiveFile);
    }

    private async Task OnActiveTabChanged(int index)
    {
        if (_disposed || _isUpdatingEditor) return;
        
        if (index >= 0 && index < ExamState.OpenFiles.Count)
        {
            var file = ExamState.OpenFiles[index];
            if (file != ExamState.ActiveFile)
            {
                ExamState.SetActiveFile(file);
            }
        }
    }

    private async Task SaveCurrentContent()
    {
        if (_disposed || ExamState.ActiveFile == null) return;

        await SaveEditorContent(ExamState.ActiveFile);
        Console.WriteLine($"💾 Saved {ExamState.ActiveFile.Name} ({ExamState.ActiveFile.Content.Length} characters)");
    }

    private async Task FormatCode()
    {
        if (_disposed || ExamState.ActiveFile?.Name.EndsWith(".java") != true) return;

        await SaveCurrentContent();
        
        var formatted = FormatJavaCode(ExamState.ActiveFile.Content);
        ExamState.ActiveFile.Content = formatted;
        fileContents[ExamState.ActiveFile.Path] = formatted;
        
        await JSRuntime.InvokeVoidAsync("window.setAceEditorContent", "ace-editor-main", formatted);
    }

    private string FormatJavaCode(string code)
    {
        var lines = code.Split('\n');
        var formatted = new List<string>();
        var indentLevel = 0;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.EndsWith("}"))
                indentLevel = Math.Max(0, indentLevel - 1);
            
            formatted.Add(new string(' ', indentLevel * 4) + trimmed);
            
            if (trimmed.EndsWith("{"))
                indentLevel++;
        }
        
        return string.Join("\n", formatted);
    }

    private string GetFileIcon(ExamFile file)
    {
        return file.Name.EndsWith(".java") ? Icons.Custom.FileFormats.FileCode : 
               file.Name.EndsWith(".csv") ? Icons.Material.Filled.TableChart : 
               Icons.Material.Filled.Description;
    }

    private string GetLanguage(ExamFile file)
    {
        return file.Name.EndsWith(".java") ? "Java" : 
               file.Name.EndsWith(".csv") ? "CSV" : "Text";
    }

    private string GetAceLanguage(ExamFile file)
    {
        return file.Name.EndsWith(".java") ? "java" : 
               file.Name.EndsWith(".csv") ? "text" : "text";
    }

    private async Task CloseFile(ExamFile file)
    {
        if (_disposed) return;

        // Save content before closing
        if (file == ExamState.ActiveFile)
        {
            await SaveEditorContent(file);
        }

        // Remove from tracking
        fileContents.Remove(file.Path);
        
        ExamState.CloseFile(file);
    }

    private int GetLineCount(string content)
    {
        if (string.IsNullOrEmpty(content)) return 1;
        return content.Split('\n').Length;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            if (ExamState != null)
            {
                ExamState.OnChange -= OnStateChanged;
            }
        }
    }
}
```
##### FullScreenWarning.razor
```
@using Microsoft.JSInterop
@inject IJSRuntime JSRuntime

@if (ShowWarning)
{
    <MudOverlay Visible="true" DarkBackground="true" Absolute="false" ZIndex="9999">
        <MudPaper Class="pa-8 ma-4" Style="max-width: 500px; text-align: center;" Elevation="24">
            <MudIcon Icon="@Icons.Material.Filled.Warning" 
                     Size="Size.Large" 
                     Color="Color.Warning" 
                     Style="font-size: 4rem; margin-bottom: 1rem;" />
            
            <MudText Typo="Typo.h4" Color="Color.Error" GutterBottom="true">
                Fullscreen Required
            </MudText>
            
            <MudText Typo="Typo.body1" Class="mb-4">
                This exam must be taken in fullscreen mode for security purposes.
            </MudText>
            
            <MudText Typo="Typo.body2" Class="mb-4">
                Please press <MudChip T="string" Size="Size.Small" Color="Color.Info">F11</MudChip> 
                or click the button below to enter fullscreen mode.
            </MudText>
            
            <MudButton Variant="Variant.Filled" 
                       Color="Color.Primary" 
                       Size="Size.Large"
                       StartIcon="@Icons.Material.Filled.Fullscreen"
                       OnClick="RequestFullscreen"
                       Class="mb-4">
                Enter Fullscreen
            </MudButton>
            
            <MudAlert Severity="Severity.Warning" Dense="true">
                <MudText Typo="Typo.caption">
                    You cannot proceed with the exam until fullscreen mode is activated.
                </MudText>
            </MudAlert>
        </MudPaper>
    </MudOverlay>
}

@code {
    [Parameter] public bool ShowWarning { get; set; } = true;
    [Parameter] public EventCallback<bool> ShowWarningChanged { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("initializeFullscreenMonitor", 
                DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public async Task OnFullscreenChanged(bool isFullscreen)
    {
        ShowWarning = !isFullscreen;
        await ShowWarningChanged.InvokeAsync(ShowWarning);
        StateHasChanged();
    }

    private async Task RequestFullscreen()
    {
        await JSRuntime.InvokeVoidAsync("requestFullscreen");
    }

    public void Dispose()
    {
        JSRuntime.InvokeVoidAsync("disposeFullscreenMonitor");
    }
}
```
##### SolutionExplorer.razor
```
@implements IDisposable
@using System.IO
@using Microsoft.AspNetCore.Components.Web
@using MudBlazor
@using AIExamIDE.Models
@using AIExamIDE.Components.Dialogs
@inject ExamState ExamState
@inject IJSRuntime JSRuntime
@inject IDialogService DialogService

<MudPaper Style="height: 100%; background-color: #fafafa;" Square="true" Elevation="0">
    <!-- Header with Actions -->
    <MudPaper Class="pa-3" Elevation="0" Square="true" Style="border-bottom: 1px solid #e0e0e0;">
        <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-2">
            <MudText Typo="Typo.h6" Class="font-weight-bold">Solution Explorer</MudText>
            <MudStack Row Spacing="1">
                <MudTooltip Text="New Folder">
                    <MudIconButton Icon="@Icons.Material.Filled.CreateNewFolder"
                                   Size="Size.Small"
                                   OnClick="CreateFolderQuick" />
                </MudTooltip>
                <MudTooltip Text="New File">
                    <MudIconButton Icon="@Icons.Material.Filled.NoteAdd"
                                   Size="Size.Small"
                                   OnClick="OpenAddFileDialog" />
                </MudTooltip>
                <MudTooltip Text="Refresh">
                    <MudIconButton Icon="@Icons.Material.Filled.Refresh"
                                   Size="Size.Small"
                                   OnClick="RefreshFiles" />
                </MudTooltip>
            </MudStack>
        </MudStack>

        <!-- Search Box -->
        <MudTextField @bind-Value="searchFilter"
                      Placeholder="Search files..."
                      Variant="Variant.Outlined"
                      Dense="true"
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      Immediate="true" />
    </MudPaper>

    <!-- File List -->
    <MudScrollArea Style="height: calc(100% - 140px);">
        <MudContainer Class="pa-2">
            @if (ExamState.Files.Any())
            {
                <MudList T="string" Dense="true" Clickable="true">
                    @foreach (var file in GetFilteredRootFiles())
                    {
                        @if (file.IsDirectory)
                        {
                            <!-- Directory -->
                            <MudListItem T="string"
                                         OnClick="() => ToggleDirectory(file.Path)"
                                         Icon="@(IsExpanded(file.Path) ? Icons.Material.Filled.FolderOpen : Icons.Material.Filled.Folder)"
                                         IconColor="Color.Primary">
                                <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Style="width: 100%;">
                                    <MudStack Row AlignItems="AlignItems.Center">
                                        <MudIcon Icon="@(IsExpanded(file.Path) ? Icons.Material.Filled.ExpandMore : Icons.Material.Filled.ChevronRight)"
                                                 Size="Size.Small" Class="mr-1" />
                                        <MudText Class="font-weight-medium">@file.Name</MudText>
                                    </MudStack>
                                    <MudStack Row Spacing="1" @onclick:stopPropagation="true">
                                        <MudTooltip Text="Rename">
                                            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                                                           Size="Size.Small"
                                                           OnClick="() => OpenRenameFileDialog(file)" />
                                        </MudTooltip>
                                        <MudTooltip Text="Delete">
                                            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                                           Size="Size.Small"
                                                           Color="Color.Error"
                                                           OnClick="() => OpenDeleteFileDialog(file)" />
                                        </MudTooltip>
                                    </MudStack>
                                </MudStack>
                            </MudListItem>

                            <!-- Directory Children -->
                            @if (IsExpanded(file.Path))
                            {
                                @foreach (var child in file.Children.OrderBy(f => f.IsDirectory ? 0 : 1).ThenBy(f => f.Name))
                                {
                                    <MudListItem T="string"
                                                 OnClick="() => ExamState.OpenFile(child)"
                                                 Icon="@GetFileIcon(child)"
                                                 Class="ml-6">
                                        <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Style="width: 100%;">
                                            <MudText>@child.Name</MudText>
                                            <MudStack Row Spacing="1" @onclick:stopPropagation="true">
                                                <MudTooltip Text="Rename">
                                                    <MudIconButton Icon="@Icons.Material.Filled.Edit"
                                                                   Size="Size.Small"
                                                                   OnClick="() => OpenRenameFileDialog(child)" />
                                                </MudTooltip>
                                                @if (!child.IsDirectory)
                                                {
                                                    <MudTooltip Text="Duplicate">
                                                        <MudIconButton Icon="@Icons.Material.Filled.ContentCopy"
                                                                       Size="Size.Small"
                                                                       OnClick="() => DuplicateFile(child)" />
                                                    </MudTooltip>
                                                }
                                                <MudTooltip Text="Delete">
                                                    <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                                                   Size="Size.Small"
                                                                   Color="Color.Error"
                                                                   OnClick="() => OpenDeleteFileDialog(child)" />
                                                </MudTooltip>
                                            </MudStack>
                                        </MudStack>
                                    </MudListItem>
                                }
                            }
                        }
                        else
                        {
                            <!-- Root Level File -->
                            <MudListItem T="string"
                                         OnClick="() => ExamState.OpenFile(file)"
                                         Icon="@GetFileIcon(file)">
                                <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Style="width: 100%;">
                                    <MudText>@file.Name</MudText>
                                    <MudStack Row Spacing="1" @onclick:stopPropagation="true">
                                        <MudTooltip Text="Rename">
                                            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                                                           Size="Size.Small"
                                                           OnClick="() => OpenRenameFileDialog(file)" />
                                        </MudTooltip>
                                        <MudTooltip Text="Duplicate">
                                            <MudIconButton Icon="@Icons.Material.Filled.ContentCopy"
                                                           Size="Size.Small"
                                                           OnClick="() => DuplicateFile(file)" />
                                        </MudTooltip>
                                        <MudTooltip Text="Delete">
                                            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                                           Size="Size.Small"
                                                           Color="Color.Error"
                                                           OnClick="() => OpenDeleteFileDialog(file)" />
                                        </MudTooltip>
                                    </MudStack>
                                </MudStack>
                            </MudListItem>
                        }
                    }
                </MudList>
            }
            else
            {
                <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center" Style="height: 200px;">
                    <MudIcon Icon="@Icons.Material.Filled.Folder" Size="Size.Large" Color="Color.Default" />
                    <MudText Typo="Typo.body1" Color="Color.Default">No files loaded</MudText>
                    <MudButton StartIcon="@Icons.Material.Filled.NoteAdd"
                               Variant="Variant.Filled"
                               Color="Color.Primary"
                               Size="Size.Small"
                               OnClick="OpenAddFileDialog">
                        Create First File
                    </MudButton>
                </MudStack>
            }
        </MudContainer>
    </MudScrollArea>

    <!-- Status Bar -->
    <MudPaper Class="pa-2" Elevation="0" Square="true" Style="border-top: 1px solid #e0e0e0;">
        <MudText Typo="Typo.caption">
            @ExamState.Files.Count file(s) | @ExamState.OpenFiles.Count open
        </MudText>
    </MudPaper>
</MudPaper>

@code {
    private string searchFilter = "";
    private HashSet<string> expandedDirectories = new();
    private bool _disposed = false;

    protected override void OnInitialized()
    {
        ExamState.OnChange += StateHasChanged;
        foreach (var f in ExamState.Files.Where(f => f.IsDirectory))
            expandedDirectories.Add(f.Path);
    }

    /* ======= Dialogs: Add / Rename / Delete ======= */

    private async Task OpenAddFileDialog()
    {
        var directories = GetAllDirectories();
var parameters = new DialogParameters
{
    { "Title", "Add a new file" },
    { "Directories", directories },
    { "DefaultDirectory", directories.FirstOrDefault() ?? "" }
};


        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true, BackdropClick = false};
        var dialog = DialogService.Show<AddFileDialog>("Add File", parameters, options);
        var result = await dialog.Result;
        if (result.Canceled) return;

        if (result.Data is AddFileDialog.AddFileResult add)
        {
            var fullPath = string.IsNullOrWhiteSpace(add.Directory)
                ? add.FileName
                : $"{add.Directory.TrimEnd('/')}/{add.FileName}";

            // Creează fișierul
            CreateFileInState(fullPath, add.InitialContent ?? string.Empty);
            // Deschide fișierul
            var created = ExamState.Files.FirstOrDefault(f => f.Path == fullPath);
            if (created != null && !created.IsDirectory)
                ExamState.OpenFile(created);

            StateHasChanged();
        }
    }

    private async Task OpenRenameFileDialog(ExamFile file)
    {
        var parameters = new DialogParameters
        {
            { "OriginalPath", file.Path }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true, CloseOnEscapeKey = true, BackdropClick = false};
        var dialog = DialogService.Show<RenameFileDialog>("Rename", parameters, options);
        var result = await dialog.Result;
        if (result.Canceled) return;

        if (result.Data is RenameFileDialog.RenameFileResult ren)
        {
            RenameInState(ren.OriginalPath, ren.NewPath);
            StateHasChanged();
        }
    }

    private async Task OpenDeleteFileDialog(ExamFile file)
    {
        var parameters = new DialogParameters
        {
            { "Path", file.Path }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true, CloseOnEscapeKey = true, BackdropClick = false};
        var dialog = DialogService.Show<DeleteFileDialog>("Delete", parameters, options);
        var result = await dialog.Result;
        if (result.Canceled) return;

        if (result.Data is DeleteFileDialog.DeleteFileResult del && del.Confirmed)
        {
            DeleteFromState(del.Path);
            StateHasChanged();
        }
    }

    private List<string> GetAllDirectories()
{
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var item in ExamState.Files)
    {
        if (item.IsDirectory && !string.IsNullOrWhiteSpace(item.Path))
            set.Add(item.Path);

        var path = item.Path ?? "";
        var idx = path.LastIndexOf('/');
        if (idx > 0)
        {
            var dir = path[..idx];
            var parts = dir.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var acc = "";
            foreach (var p in parts)
            {
                acc = string.IsNullOrEmpty(acc) ? p : $"{acc}/{p}";
                set.Add(acc);
            }
        }
    }

    return set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
}


    /* ======= State helpers ======= */

    private void CreateFileInState(string path, string content)
    {
        // dacă directorul nu există ca item, îl adăugăm
        var dirPath = GetDirectoryPart(path);
        if (!string.IsNullOrEmpty(dirPath) && !ExamState.Files.Any(f => f.IsDirectory && f.Path == dirPath))
        {
            ExamState.Files.Add(new ExamFile
            {
                Name = Path.GetFileName(dirPath),
                Path = dirPath,
                IsDirectory = true,
                Children = new List<ExamFile>()
            });
        }

        var file = new ExamFile
        {
            Name = Path.GetFileName(path),
            Path = path,
            IsDirectory = false,
            Content = string.IsNullOrEmpty(content) ? GetDefaultContent(Path.GetFileName(path)) : content,
            Children = new List<ExamFile>()
        };

        ExamState.Files.Add(file);

        // leagă în arborele directorului dacă există fizic în listă
        var parent = ExamState.Files.FirstOrDefault(f => f.IsDirectory && f.Path == dirPath);
        parent?.Children.Add(file);
    }

    private void RenameInState(string oldPath, string newPath)
    {
        var item = ExamState.Files.FirstOrDefault(f => f.Path == oldPath);
        if (item == null) return;

        if (!item.IsDirectory)
        {
            item.Name = Path.GetFileName(newPath);
            item.Path = newPath;
        }
        else
        {
            var oldPrefix = oldPath.TrimEnd('/') + "/";
            var newPrefix = newPath.TrimEnd('/') + "/";

            item.Name = Path.GetFileName(newPath);
            item.Path = newPath;

            // actualizează toate fișierele din director recursiv
            foreach (var f in ExamState.Files.Where(f => f.Path.StartsWith(oldPrefix) && f.Path != oldPath))
            {
                f.Path = newPrefix + f.Path.Substring(oldPrefix.Length);
            }
            // reconstruiește Children minimale pentru folderul mutat
            item.Children = ExamState.Files
                .Where(f => f.Path.StartsWith(newPrefix) && f.Path != newPath && !f.Path.Substring(newPrefix.Length).Contains('/'))
                .ToList();
        }

        // închide filele deschise care s-au mutat și le redeschide cu noul path
        var affectedOpen = ExamState.OpenFiles.Where(of => of.Path == oldPath || of.Path.StartsWith(oldPath.TrimEnd('/') + "/")).ToList();
        foreach (var of in affectedOpen)
            ExamState.CloseFile(of);

        var reopened = ExamState.Files.Where(f => f.Path == newPath || f.Path.StartsWith(newPath.TrimEnd('/') + "/") && !f.IsDirectory);
        foreach (var f in reopened)
            ExamState.OpenFile(f);
    }

    private void DeleteFromState(string targetPath)
    {
        var target = ExamState.Files.FirstOrDefault(f => f.Path == targetPath);
        if (target == null) return;

        if (!target.IsDirectory)
        {
            ExamState.CloseFile(target);
            ExamState.Files.Remove(target);
            // scoate din Children ale părintelui dacă există
            var parent = ExamState.Files.FirstOrDefault(f => f.IsDirectory && f.Path == GetDirectoryPart(targetPath));
            parent?.Children.RemoveAll(c => c.Path == targetPath);
        }
        else
        {
            var prefix = targetPath.TrimEnd('/') + "/";
            var toRemove = ExamState.Files.Where(f => f.Path == targetPath || f.Path.StartsWith(prefix)).ToList();

            foreach (var f in toRemove)
                ExamState.CloseFile(f);

            // șterge din children ale părintelui
            var parent = ExamState.Files.FirstOrDefault(f => f.IsDirectory && f.Path == GetDirectoryPart(targetPath));
            parent?.Children.RemoveAll(c => c.Path == targetPath);

            foreach (var f in toRemove)
                ExamState.Files.Remove(f);
        }
    }

    private static string GetDirectoryPart(string path)
    {
        var idx = path.LastIndexOf('/');
        return idx <= 0 ? "" : path.Substring(0, idx);
    }

private string GetDefaultContent(string filePath)
{
    if (filePath.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
    {
        var className = Path.GetFileNameWithoutExtension(filePath);
        var dir = Path.GetDirectoryName(filePath)?.Replace("\\", "/") ?? string.Empty;

        string packageLine = "";
        if (!string.IsNullOrWhiteSpace(dir))
        {
            var parts = dir.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // construiește pachetul din segmentele după "src"
            var pkg = string.Join(".", parts.SkipWhile(p => p.Equals("src", StringComparison.OrdinalIgnoreCase)))
                             .ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(pkg))
                packageLine = $"package {pkg};\n\n";
        }

        return $@"{packageLine}public class {className} {{

    public static void main(String[] args) {{
        System.out.println(""Hello from {className}!"");
    }}
}}";
    }

    if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        return "Column1,Column2,Column3\nValue1,Value2,Value3";

    return "";
}


    /* ======= UI helpers ======= */

    private bool IsExpanded(string directoryPath) => expandedDirectories.Contains(directoryPath);

    private void ToggleDirectory(string directoryPath)
    {
        if (expandedDirectories.Contains(directoryPath))
            expandedDirectories.Remove(directoryPath);
        else
            expandedDirectories.Add(directoryPath);

        StateHasChanged();
    }

    private List<ExamFile> GetFilteredRootFiles()
    {
        var root = GetRootFiles();
        if (string.IsNullOrWhiteSpace(searchFilter)) return root;

        return root.Where(f =>
            f.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
            (f.IsDirectory && f.Children.Any(c => c.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)))
        ).ToList();
    }

    private List<ExamFile> GetRootFiles()
{
    // colecție de noduri de directoare la rădăcină
    var dirNodes = new Dictionary<string, ExamFile>(StringComparer.OrdinalIgnoreCase);
    // fișiere la rădăcină
    var rootFiles = new List<ExamFile>();

    // 1) preînregistrează directoarele top-level existente ca noduri
    foreach (var item in ExamState.Files)
    {
        var parts = item.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1 && item.IsDirectory)
        {
            if (!dirNodes.ContainsKey(item.Path))
            {
                // folosește nodul existent ca bază, dar cu Children resetat pentru afișare
                dirNodes[item.Path] = new ExamFile
                {
                    Name = item.Name,
                    Path = item.Path,
                    IsDirectory = true,
                    Children = new List<ExamFile>()
                };
            }
        }
    }

    // 2) parcurge toate itemele și le plasează corect
    foreach (var item in ExamState.Files)
    {
        var parts = item.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // fișier la rădăcină
        if (parts.Length == 1 && !item.IsDirectory)
        {
            rootFiles.Add(item);
            continue;
        }

        // orice subelement într-un folder top-level
        if (parts.Length > 1)
        {
            var top = parts[0];

            // dacă nu există un folder top-level deja, creează un nod sintetic O SINGURĂ DATĂ
            if (!dirNodes.TryGetValue(top, out var topNode))
            {
                topNode = new ExamFile
                {
                    Name = top,
                    Path = top,
                    IsDirectory = true,
                    Children = new List<ExamFile>()
                };
                dirNodes[top] = topNode;
            }

            // adaugă doar copiii direcți ai folderului top-level
            // exemplu: pentru "src/Main.java" adaugă "Main.java"
            // pentru "src/sub/Deep.java" nu-l adăugăm aici, va apărea când extinzi ulterior structura
            if (parts.Length == 2)
            {
                topNode.Children.Add(item);
            }
        }
    }

    // 3) compune lista finală: directoarele (unice) + fișierele root
    var result = new List<ExamFile>();
    result.AddRange(dirNodes.Values.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase));
    result.AddRange(rootFiles.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase));
    return result;
}

    private List<ExamFile> GetDirectories()
        => ExamState.Files.Where(f => f.IsDirectory).ToList();

    private string GetFileIcon(ExamFile file)
    {
        if (file.IsDirectory) return Icons.Custom.Uncategorized.Folder;
        if (file.Name.EndsWith(".java", StringComparison.OrdinalIgnoreCase)) return Icons.Custom.FileFormats.FileCode;
        if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return Icons.Material.Filled.TableChart;
        if (file.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) return Icons.Material.Filled.TextSnippet;
        return Icons.Material.Filled.Description;
    }

    private void DuplicateFile(ExamFile file)
    {
        if (file.IsDirectory) return;

        var baseName = Path.GetFileNameWithoutExtension(file.Name);
        var ext = Path.GetExtension(file.Name);
        var copyName = $"{baseName}_copy{ext}";
        var parts = file.Path.Split('/');
        parts[^1] = copyName;
        var copyPath = string.Join("/", parts);

        // dacă există deja, incrementează sufixul
        int i = 2;
        while (ExamState.Files.Any(f => f.Path == copyPath))
        {
            copyName = $"{baseName}_copy{i}{ext}";
            parts[^1] = copyName;
            copyPath = string.Join("/", parts);
            i++;
        }

        var duplicate = new ExamFile
        {
            Name = copyName,
            Path = copyPath,
            Content = file.Content,
            IsDirectory = false,
            Children = new List<ExamFile>()
        };

        ExamState.Files.Add(duplicate);

        var parent = ExamState.Files.FirstOrDefault(f => f.IsDirectory && f.Path == GetDirectoryPart(copyPath));
        parent?.Children.Add(duplicate);

        StateHasChanged();
    }

    private void RefreshFiles() => StateHasChanged();

    /* ======= Quick create folder (no modal) ======= */
    private void CreateFolderQuick()
    {
        var baseName = "NewFolder";
        var name = baseName;
        var path = name;

        int i = 2;
        while (ExamState.Files.Any(f => f.Path == path))
        {
            name = $"{baseName}{i}";
            path = name;
            i++;
        }

        var folder = new ExamFile
        {
            Name = name,
            Path = path,
            IsDirectory = true,
            Children = new List<ExamFile>()
        };
        ExamState.Files.Add(folder);
        expandedDirectories.Add(folder.Path);
        StateHasChanged();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (ExamState != null)
                ExamState.OnChange -= StateHasChanged;
        }
    }
}
```
##### TaskPanel.razor
```
@implements IDisposable
@inject ExamState ExamState
@inject ApiClient ApiClient
@inject IJSRuntime JSRuntime

<style>
.timer-box {
    padding: 6px 16px;
    border-radius: 16px;
    font-weight: 700;
    font-family: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, "Liberation Mono", monospace;
    letter-spacing: 0.8px;
    box-shadow: 
        0 0 0 1px var(--mud-palette-lines-default) inset,
        0 2px 8px rgba(0, 0, 0, 0.08),
        0 1px 3px rgba(0, 0, 0, 0.12);
    transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    position: relative;
    overflow: hidden;
    backdrop-filter: blur(8px);
}

.timer-box::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(135deg, rgba(255, 255, 255, 0.1) 0%, rgba(255, 255, 255, 0.05) 100%);
    pointer-events: none;
}

.timer-box:hover {
    transform: translateY(-1px);
    box-shadow: 
        0 0 0 1px var(--mud-palette-lines-default) inset,
        0 4px 16px rgba(0, 0, 0, 0.12),
        0 2px 6px rgba(0, 0, 0, 0.16);
}

.timer-text {
    font-size: 1.4rem;
    line-height: 1.1;
    position: relative;
    z-index: 1;
    color: white;
    text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}

.timer-ok {
    background: linear-gradient(135deg, var(--mud-palette-success) 0%, color-mix(in oklab, var(--mud-palette-success) 85%, black) 100%);
    color: white;
    border: 1px solid color-mix(in oklab, var(--mud-palette-success) 80%, white);
}

.timer-warn {
    background: linear-gradient(135deg, var(--mud-palette-warning) 0%, color-mix(in oklab, var(--mud-palette-warning) 90%, black) 100%);
    color: white;
    box-shadow: 
        0 0 0 2px color-mix(in oklab, var(--mud-palette-warning) 45%, transparent) inset,
        0 2px 8px rgba(0, 0, 0, 0.08),
        0 1px 3px rgba(0, 0, 0, 0.12),
        0 0 20px color-mix(in oklab, var(--mud-palette-warning) 30%, transparent);
    border: 1px solid color-mix(in oklab, var(--mud-palette-warning) 70%, white);
}

.timer-crit {
    background: linear-gradient(135deg, var(--mud-palette-error) 0%, color-mix(in oklab, var(--mud-palette-error) 85%, black) 100%);
    color: white;
    border: 1px solid color-mix(in oklab, var(--mud-palette-error) 80%, white);
    box-shadow: 
        0 0 0 1px var(--mud-palette-error) inset,
        0 2px 8px rgba(0, 0, 0, 0.08),
        0 1px 3px rgba(0, 0, 0, 0.12),
        0 0 25px color-mix(in oklab, var(--mud-palette-error) 40%, transparent);
}

.timer-pulse {
    animation: timer-pulse 1200ms cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

.timer-crit.timer-pulse {
    animation: timer-pulse-urgent 800ms cubic-bezier(0.25, 0.46, 0.45, 0.94) infinite;
}

@@keyframes timer-pulse {
    0% { 
        transform: scale(1);
        filter: brightness(1);
    }
    50% { 
        transform: scale(1.04);
        filter: brightness(1.1);
    }
    100% { 
        transform: scale(1);
        filter: brightness(1);
    }
}

@@keyframes timer-pulse-urgent {
    0% { 
        transform: scale(1);
        filter: brightness(1) saturate(1);
        box-shadow: 
            0 0 0 1px var(--mud-palette-error) inset,
            0 2px 8px rgba(0, 0, 0, 0.08),
            0 1px 3px rgba(0, 0, 0, 0.12),
            0 0 25px color-mix(in oklab, var(--mud-palette-error) 40%, transparent);
    }
    50% { 
        transform: scale(1.08);
        filter: brightness(1.15) saturate(1.2);
        box-shadow: 
            0 0 0 2px var(--mud-palette-error) inset,
            0 4px 16px rgba(0, 0, 0, 0.15),
            0 2px 6px rgba(0, 0, 0, 0.2),
            0 0 35px color-mix(in oklab, var(--mud-palette-error) 60%, transparent);
    }
    100% { 
        transform: scale(1);
        filter: brightness(1) saturate(1);
        box-shadow: 
            0 0 0 1px var(--mud-palette-error) inset,
            0 2px 8px rgba(0, 0, 0, 0.08),
            0 1px 3px rgba(0, 0, 0, 0.12),
            0 0 25px color-mix(in oklab, var(--mud-palette-error) 40%, transparent);
    }
}

/* Optional: Add a subtle loading state */
.timer-loading {
    position: relative;
    overflow: hidden;
}

.timer-loading::after {
    content: '';
    position: absolute;
    top: 0;
    left: -100%;
    width: 100%;
    height: 100%;
    background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
    animation: timer-shimmer 2s infinite;
}

@@keyframes timer-shimmer {
    0% { left: -100%; }
    100% { left: 100%; }
}
</style>

<MudPaper Square="true" Elevation="0">
    <MudStack  Spacing="0">
        <!-- Header sticky -->
        <MudPaper Elevation="0"
                  Class="px-4 py-3"
                  Style="position:sticky;top:0;z-index:1;background:var(--mud-palette-background);border-bottom:1px solid var(--mud-palette-lines-default);">
            <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                <MudText Typo="Typo.h6" Class="font-weight-bold">Tasks</MudText>

                <!-- TIMER (not a MudChip) -->
                <MudPaper Elevation="1"
                          Class="@GetTimerBoxClass()"
                          Role="status"
                          AriaLabel="Time remaining">
                    <MudStack Row AlignItems="AlignItems.Center" Spacing="1">
                        <MudIcon Icon="@Icons.Material.Filled.Timer" />
                        <MudText Class="timer-text">@FormatTime(ExamState.TimeRemainingSeconds)</MudText>
                    </MudStack>
                </MudPaper>
            </MudStack>
        </MudPaper>

        <!-- Scrollable content -->
        <MudScrollArea Class="px-4 py-3" Style="height:100%;min-height:0;">
            @if (ExamState.CurrentExam != null)
            {
                <!-- Current Task -->
                <MudCard Class="mb-3" Elevation="1" Dense="true">
                    <MudCardHeader DisableTypography="true">
                        <CardHeaderContent>
                            <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                                <MudText Typo="Typo.subtitle1" Class="font-weight-bold">
                                    Task @(ExamState.CurrentTaskIndex + 1) / @ExamState.CurrentExam.Tasks.Count
                                </MudText>
                                <MudChip T="string" Size="Size.Small" Color="Color.Primary" Variant="Variant.Text">
                                    @GetTaskProgress()
                                </MudChip>
                            </MudStack>
                        </CardHeaderContent>
                    </MudCardHeader>

                    <MudDivider />

                    <MudCardContent Class="py-2">
                        @if (CurrentTask != null)
                        {
                            <MudText Typo="Typo.subtitle2" Class="font-weight-medium mb-1">@CurrentTask.Title</MudText>
                            <MudText Typo="Typo.body2" Class="text-secondary">@CurrentTask.Description</MudText>
                        }
                    </MudCardContent>

                    <MudCardActions Class="px-3 pb-3 pt-0">
                        <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="w-100">
                            <MudButton StartIcon="@Icons.Material.Filled.ArrowBack"
                                       Variant="Variant.Outlined"
                                       Size="Size.Small"
                                       OnClick="PreviousTask"
                                       Disabled="@(ExamState.CurrentTaskIndex == 0)">
                                Previous
                            </MudButton>

                            <MudButton EndIcon="@Icons.Material.Filled.ArrowForward"
                                       Variant="Variant.Outlined"
                                       Color="Color.Primary"
                                       Size="Size.Small"
                                       OnClick="NextTask"
                                       Disabled="@(ExamState.CurrentTaskIndex >= ExamState.CurrentExam.Tasks.Count - 1)">
                                Next
                            </MudButton>
                        </MudStack>
                    </MudCardActions>
                </MudCard>

                <!-- CSV Data Files -->
                @if (GetCsvFiles().Any())
                {
                    <MudCard Class="mb-3" Elevation="1" Dense="true">
                        <MudCardHeader DisableTypography="true">
                            <CardHeaderContent>
                                <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                                    <MudIcon Icon="@Icons.Material.Filled.TableChart" Color="Color.Warning" />
                                    <MudText Typo="Typo.subtitle1" Class="font-weight-medium">CSV Data Files</MudText>
                                    <MudChip T="string" Size="Size.Small" Color="Color.Warning" Variant="Variant.Text">
                                        @GetCsvFiles().Count file(s)
                                    </MudChip>
                                </MudStack>
                            </CardHeaderContent>
                        </MudCardHeader>

                        <MudDivider />

                        <MudCardContent Class="py-2">
                            <MudStack Spacing="1">
                                @foreach (var csvFile in GetCsvFiles())
                                {
                                    <MudPaper Class="pa-2" Elevation="0" Style="border-left:4px solid var(--mud-palette-warning);">
                                        <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                                            <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                                                <MudIcon Icon="@Icons.Material.Filled.TableChart" Size="Size.Small" />
                                                <MudText Typo="Typo.body2" Class="font-weight-medium">@csvFile.Name</MudText>
                                            </MudStack>

                                            <MudStack Row Spacing="1">
                                                <MudButton StartIcon="@Icons.Material.Filled.Visibility"
                                                           Size="Size.Small"
                                                           Variant="Variant.Filled"
                                                           Color="Color.Info"
                                                           OnClick="() => ShowCsvOverlay(csvFile)">
                                                    View
                                                </MudButton>
                                                <MudIconButton Icon="@Icons.Material.Filled.ContentCopy"
                                                               Size="Size.Small"
                                                               Color="Color.Secondary"
                                                               AriaLabel="Copy path"
                                                               OnClick="() => CopyPath(csvFile.Path)" />
                                            </MudStack>
                                        </MudStack>
                                    </MudPaper>
                                }
                            </MudStack>
                        </MudCardContent>
                    </MudCard>
                }

                <!-- Run Configuration -->
                @if (ExamState.GetRunnableFiles().Any())
                {
                    <MudCard Class="mb-3" Elevation="1" Dense="true">
                        <MudCardHeader DisableTypography="true">
                            <CardHeaderContent>
                                <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                                    <MudIcon Icon="@Icons.Material.Filled.PlayArrow" Color="Color.Success" />
                                    <MudText Typo="Typo.subtitle1" Class="font-weight-medium">Run Configuration</MudText>
                                </MudStack>
                            </CardHeaderContent>
                        </MudCardHeader>

                        <MudDivider />

                        <MudCardContent Class="py-2">
                            <MudStack Spacing="2">
                                @if (ExamState.ActiveFile != null && ExamState.HasMainMethod(ExamState.ActiveFile))
                                {
                                    <MudAlert Severity="Severity.Success" Dense="true">
                                        <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                                            <MudText Typo="Typo.body2">
                                                Current File:
                                                <MudText Typo="Typo.body2" Class="font-weight-bold d-inline"> @ExamState.ActiveFile.Name </MudText>
                                                has main()
                                            </MudText>
                                        </MudStack>
                                    </MudAlert>
                                }

                                <MudSelect @bind-Value="selectedRunFile"
                                           Variant="Variant.Outlined"
                                           Dense="true"
                                           >
                                    <MudSelectItem Value="@("")">Auto detect</MudSelectItem>
                                    @foreach (var file in ExamState.GetRunnableFiles())
                                    {
                                        <MudSelectItem Value="@file.Path">
                                            <MudStack Row AlignItems="AlignItems.Center" Spacing="1">
                                                <MudIcon Icon="@Icons.Custom.FileFormats.FileCode" Size="Size.Small" />
                                                <MudText Typo="Typo.body2">@file.Name</MudText>
                                            </MudStack>
                                        </MudSelectItem>
                                    }
                                </MudSelect>

                                <MudText Typo="Typo.caption" Class="text-secondary">
                                    <MudIcon Icon="@Icons.Material.Filled.Info" Size="Size.Small" Class="mr-1" />
                                    Will run:
                                    <MudText Typo="Typo.caption" Class="font-weight-bold d-inline"> @GetAutoDetectedFile() </MudText>
                                </MudText>
                            </MudStack>
                        </MudCardContent>
                    </MudCard>
                }

                <!-- Actions -->
                <MudCard Class="mb-3" Elevation="1" Dense="true">
                    <MudCardHeader DisableTypography="true">
                        <CardHeaderContent>
                            <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                                <MudIcon Icon="@Icons.Material.Filled.Settings" />
                                <MudText Typo="Typo.subtitle1" Class="font-weight-medium">Actions</MudText>
                            </MudStack>
                        </CardHeaderContent>
                    </MudCardHeader>

                    <MudDivider />

                    <MudCardContent Class="py-2">
                        <MudStack Spacing="1">
                            <MudButton StartIcon="@Icons.Material.Filled.PlayArrow"
                                       Variant="Variant.Filled"
                                       Color="Color.Success"
                                       FullWidth="true"
                                       Size="Size.Medium"
                                       OnClick="RunCode"
                                       Disabled="@(isRunning || !HasRunnableFile() || ExamState.IsSubmitted)">
                                @if (isRunning)
                                {
                                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                                    <span>Running...</span>
                                }
                                else
                                {
                                    <span>Run @GetRunButtonText()</span>
                                }
                            </MudButton>

                            <MudButton StartIcon="@Icons.Material.Filled.Refresh"
                                       Variant="Variant.Filled"
                                       Color="Color.Warning"
                                       FullWidth="true"
                                       OnClick="ResetCode"
                                       Disabled="@ExamState.IsSubmitted">
                                Reset All Files
                            </MudButton>

                            <MudButton StartIcon="@Icons.Material.Filled.Send"
                                       Variant="Variant.Filled"
                                       Color="Color.Error"
                                       FullWidth="true"
                                       OnClick="SubmitExam"
                                       Disabled="@ExamState.IsSubmitted">
                                Submit Exam
                            </MudButton>
                        </MudStack>
                    </MudCardContent>
                </MudCard>

                <!-- Runnable Files -->
                <MudCard Elevation="1" Dense="true">
                    <MudCardHeader DisableTypography="true">
                        <CardHeaderContent>
                            <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                                <MudIcon Icon="@Icons.Custom.FileFormats.FileCode" Color="Color.Info" />
                                <MudText Typo="Typo.subtitle1" Class="font-weight-medium">Runnable Files</MudText>
                                <MudChip T="string" Size="Size.Small" Color="Color.Info" Variant="Variant.Text">
                                    @ExamState.GetRunnableFiles().Count() file(s)
                                </MudChip>
                            </MudStack>
                        </CardHeaderContent>
                    </MudCardHeader>

                    <MudDivider />

                    <MudCardContent Class="py-2">
                        @if (ExamState.GetRunnableFiles().Any())
                        {
                            <MudStack Spacing="1">
                                @foreach (var file in ExamState.GetRunnableFiles())
                                {
                                    <MudPaper Class="pa-2" Elevation="0" Style="border-left:4px solid var(--mud-palette-info);">
                                        <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                                            <MudButton StartIcon="@Icons.Custom.FileFormats.FileCode"
                                                       Variant="Variant.Text"
                                                       Size="Size.Small"
                                                       OnClick="() => ExamState.OpenFile(file)">
                                                @file.Name
                                            </MudButton>
                                            <MudIconButton Icon="@Icons.Material.Filled.PlayArrow"
                                                           Size="Size.Small"
                                                           Color="Color.Success"
                                                           AriaLabel="Run this file"
                                                           OnClick="() => RunSpecificFile(file)"
                                                           Disabled="@ExamState.IsSubmitted" />
                                        </MudStack>
                                    </MudPaper>
                                }
                            </MudStack>
                        }
                        else
                        {
                            <MudAlert Severity="Severity.Info" Dense="true">
                                <MudText>No runnable files found. Create a Java file with a main() method.</MudText>
                            </MudAlert>
                        }
                    </MudCardContent>
                </MudCard>
            }
        </MudScrollArea>
    </MudStack>
</MudPaper>

@code {
    private bool isRunning = false;
    private string selectedRunFile = "";
    private bool _disposed = false;
    private ExamTask? CurrentTask => ExamState.CurrentExam?.Tasks.ElementAtOrDefault(ExamState.CurrentTaskIndex);

    protected override void OnInitialized()
    {
        ExamState.OnChange += StateHasChanged;
    }

    private List<ExamFile> GetCsvFiles()
    {
        return ExamState.Files.Where(f => !f.IsDirectory && f.Name.EndsWith(".csv")).ToList();
    }

    private string GetTaskProgress()
    {
        if (ExamState.CurrentExam == null) return "0%";
        var progress = ((ExamState.CurrentTaskIndex + 1) * 100) / ExamState.CurrentExam.Tasks.Count;
        return $"{progress}%";
    }

    private string GetTimerBoxClass()
    {
        if (ExamState.TimeRemainingSeconds <= 60)
            return "timer-box timer-crit timer-pulse";
        if (ExamState.TimeRemainingSeconds <= 300)
            return "timer-box timer-warn";
        return "timer-box timer-ok";
    }

    private void ShowCsvOverlay(ExamFile csvFile)
    {
        JSRuntime.InvokeVoidAsync("eval", $"window.blazorCulture.showCsvOverlay('{csvFile.Name}', '{csvFile.Path}', '{csvFile.Content}')");
        ExamState.ShowCsvOverlay(csvFile);
    }

    private async Task CopyPath(string path)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", path);
            Console.WriteLine($"Path copied to clipboard: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to copy path: {ex.Message}");
        }
    }

    private bool HasRunnableFile() => ExamState.GetRunnableFiles().Any();

    private string GetAutoDetectedFile()
    {
        if (ExamState.ActiveFile != null && ExamState.HasMainMethod(ExamState.ActiveFile))
            return ExamState.ActiveFile.Name;

        var mainJava = ExamState.GetFileByPath("src/Main.java");
        if (mainJava != null && ExamState.HasMainMethod(mainJava))
            return "Main.java";

        var firstRunnable = ExamState.GetRunnableFiles().FirstOrDefault();
        return firstRunnable?.Name ?? "None";
    }

    private string GetRunButtonText()
    {
        if (!string.IsNullOrEmpty(selectedRunFile))
        {
            var file = ExamState.GetFileByPath(selectedRunFile);
            return file?.Name ?? "File";
        }
        return GetAutoDetectedFile();
    }

    private async Task RunCode()
    {
        if (_disposed) return;
        ExamState.NotifyCodeRunStarted();
        isRunning = true;
        StateHasChanged();

        try
        {
            await Task.Delay(100);
            var files = ExamState.GetAllFiles();
            var mainFile = GetFileToRun();

            if (mainFile == null)
            {
                ExamState.SetConsoleOutput("Error: No runnable file found. Make sure your Java file has a main() method.");
                return;
            }

            Console.WriteLine($"🚀 Running {mainFile.Name} with content length: {mainFile.Content.Length}");

            var result = await ApiClient.RunCodeAsync(files, mainFile.Path);

            var output = !string.IsNullOrEmpty(result.Error)
                ? $"=== Running {mainFile.Name} ===\n\nError:\n{result.Error}"
                : $"=== Running {mainFile.Name} ===\n\nOutput:\n{result.Output}";

            ExamState.SetConsoleOutput(output);
        }
        catch (Exception ex)
        {
            ExamState.SetConsoleOutput($"Error: {ex.Message}");
        }
        finally
        {
            isRunning = false;
            if (!_disposed) StateHasChanged();
        }
    }

    private async Task RunSpecificFile(ExamFile file)
    {
        selectedRunFile = file.Path;
        await RunCode();
    }

    private ExamFile? GetFileToRun()
    {
        if (!string.IsNullOrEmpty(selectedRunFile))
            return ExamState.GetFileByPath(selectedRunFile);

        if (ExamState.ActiveFile != null && ExamState.HasMainMethod(ExamState.ActiveFile))
            return ExamState.ActiveFile;

        var mainJava = ExamState.GetFileByPath("src/Main.java");
        if (mainJava != null && ExamState.HasMainMethod(mainJava))
            return mainJava;

        return ExamState.GetRunnableFiles().FirstOrDefault();
    }

    private async Task ResetCode()
    {
        if (_disposed) return;

        try
        {
            await ApiClient.ResetExamAsync();
            var examData = await ApiClient.GenerateExamAsync();
            ExamState.LoadExam(examData.Exam, examData.Files);
            ExamState.SetConsoleOutput("All files reset to initial state.");
        }
        catch (Exception ex)
        {
            ExamState.SetConsoleOutput($"Reset failed: {ex.Message}");
        }
    }

    private async Task SubmitExam()
    {
        if (_disposed) return;

        try
        {
            var files = ExamState.GetAllFiles();
            await ApiClient.SubmitExamAsync(files);
            ExamState.MarkAsSubmitted();
            ExamState.SetConsoleOutput("Exam submitted successfully!");
        }
        catch (Exception ex)
        {
            ExamState.SetConsoleOutput($"Submit failed: {ex.Message}");
        }
    }

    private void PreviousTask()
    {
        if (_disposed) return;
        if (ExamState.CurrentTaskIndex > 0)
        {
            ExamState.CurrentTaskIndex--;
            StateHasChanged();
        }
    }

    private void NextTask()
    {
        if (_disposed) return;
        if (ExamState.CurrentExam != null && ExamState.CurrentTaskIndex < ExamState.CurrentExam.Tasks.Count - 1)
        {
            ExamState.CurrentTaskIndex++;
            StateHasChanged();
        }
    }

    private string FormatTime(int seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (ExamState != null)
            {
                ExamState.OnChange -= StateHasChanged;
            }
        }
    }
}
```
#### Pages folder
##### Home.razor
```
@page "/"
@using AIExamIDE.Services
@using AIExamIDE.Models
@inject ExamState ExamState
@inject ApiClient ApiClient
@inject IJSRuntime JSRuntime
@rendermode InteractiveServer

<PageTitle>PePExam - @FormatTime(ExamState.TimeRemainingSeconds) Remaining</PageTitle>

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
<FullscreenWarning @bind-ShowWarning="showFullscreenWarning" />

@if (!showFullscreenWarning)
{
    <!-- Loading Overlay -->
    @if (_isLoading)
    {
        <div
            style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 9999; background-color: rgba(0,0,0,0.8); display: flex; align-items: center; justify-content: center;">
            <MudPaper Class="pa-8" Elevation="8" Style="border-radius: 16px; text-align: center; max-width: 500px;">
                <MudStack AlignItems="AlignItems.Center" Spacing="4">
                    <MudProgressCircular Size="Size.Large" Indeterminate="true" Color="Color.Primary" />
                    <MudText Typo="Typo.h4" Class="font-weight-bold">Generating Your Exam</MudText>
                    <MudText Typo="Typo.body1" Style="color: #666;">
                        🤖 AI is creating a personalized programming exam with tasks and data files...
                    </MudText>
                    <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                        <MudIcon Icon="@Icons.Material.Filled.AutoAwesome" Color="Color.Primary" />
                        <MudText Typo="Typo.body2">Powered by GPT-5</MudText>
                    </MudStack>
                    <MudLinearProgress Indeterminate="true" Color="Color.Primary" Style="width: 100%;" />
                    <MudText Typo="Typo.caption" Style="color: #888;">
                        This may take a few moments...
                    </MudText>
                </MudStack>
            </MudPaper>
        </div>
    }

    <MudLayout>
        <!-- Header -->
        <MudAppBar Elevation="1" Dense="true" Color="Color.Primary">
            <MudText Typo="Typo.h6" Class="text-white font-weight-bold">
                Programming Examination Platform v5.0
            </MudText>
            <MudSpacer />
            <!-- Console Toggle Button -->
            <MudIconButton Icon="@Icons.Material.Filled.Terminal" Color="Color.Inherit" OnClick="ToggleConsole" />
        </MudAppBar>

        <!-- Main IDE Split -->
        <MudMainContent>
            @if (!_isLoading && ExamState.CurrentExam != null)
            {
                <MudGrid Spacing="0" Class="h-100">

                    <!-- Solution Explorer -->
                    <MudItem xs="2" Class="border-right bg-gray-100">
                        <SolutionExplorer />
                    </MudItem>

                    <!-- Code Editor -->
                    <MudItem xs="7" Class="bg-dark">
                        <EditorTabs />
                    </MudItem>

                    <!-- Task Panel -->
                    <MudItem xs="3" Class="border-left bg-gray-50">
                        <TaskPanel />
                    </MudItem>
                </MudGrid>
            }
            else if (!_isLoading)
            {
                <div style="display: flex; align-items: center; justify-content: center; height: 100%;">
                    <MudPaper Class="pa-8" Elevation="4" Style="text-align: center;">
                        <MudStack AlignItems="AlignItems.Center" Spacing="3">
                            <MudIcon Icon="@Icons.Material.Filled.Error" Size="Size.Large" Color="Color.Error" />
                            <MudText Typo="Typo.h5">Failed to Load Exam</MudText>
                            <MudText Typo="Typo.body1">There was an error generating your exam. Please refresh the page to try
                                again.</MudText>
                            <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="RefreshPage">
                                Refresh Page
                            </MudButton>
                        </MudStack>
                    </MudPaper>
                </div>
            }
        </MudMainContent>
    </MudLayout>

    <!-- Console Overlay -->
    @if (_isConsoleOpen)
    {
        <div
            style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 3000; display: flex; align-items: center; justify-content: center; pointer-events: none;">
            <MudPaper Class="pa-0"
                Style="width: 95vw; height: 60vh; max-width: 1400px; display: flex; flex-direction: column; pointer-events: auto;"
                Elevation="8">
                <!-- Console Header -->
                <MudPaper Class="pa-3" Elevation="0"
                    Style="background-color: #1e1e1e; color: white; border-radius: 4px 4px 0 0;">
                    <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                        <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                            <MudIcon Icon="@Icons.Material.Filled.Terminal" />
                            <MudText Typo="Typo.h6">Console Output</MudText>

                            <!-- Timer as MudChip -->
                            <MudChip T="string" Color="@GetTimerChipColor()" Size="Size.Small"
                                Icon="@Icons.Material.Filled.Timer" Style="font-family: monospace; font-weight: bold;">
                                @FormatTime(ExamState.TimeRemainingSeconds)
                            </MudChip>
                        </MudStack>

                        <MudStack Row Spacing="1">
                            <MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Inherit" Size="Size.Small"
                                OnClick="ClearConsole" />
                            <MudIconButton Icon="@Icons.Material.Filled.Minimize" Color="Color.Inherit" Size="Size.Small"
                                OnClick="MinimizeConsole" />
                            <MudIconButton Icon="@Icons.Material.Filled.Close" Color="Color.Inherit" Size="Size.Small"
                                OnClick="CloseConsole" />
                        </MudStack>
                    </MudStack>
                </MudPaper>

                <!-- Console Content -->
                <div style="flex: 1; overflow: hidden; background-color: #0d1117;">
                    <div style="height: 100%; padding: 16px; overflow-y: auto;">
                        <div
                            style="font-family: 'Consolas', 'Monaco', 'Courier New', monospace; font-size: 14px; line-height: 1.4; white-space: pre-wrap; word-break: break-word;">
                            @if (string.IsNullOrEmpty(ExamState.ConsoleOutput))
                            {
                                <div style="color: #7d8590; font-style: italic;">
                                    Console output will appear here...
                                </div>
                            }
                            else
                            {
                                @((MarkupString)FormatConsoleOutput(ExamState.ConsoleOutput))
                            }
                        </div>
                    </div>
                </div>

                <!-- Console Footer -->
                <MudPaper Class="pa-2" Elevation="0" Style="background-color: #21262d; border-radius: 0 0 4px 4px;">
                    <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                        <MudText Typo="Typo.caption" Style="color: #7d8590;">
                            @if (!string.IsNullOrEmpty(ExamState.ConsoleOutput))
                            {
                                <span>Output ready • @DateTime.Now.ToString("HH:mm:ss")</span>
                            }
                            else
                            {
                                <span>Ready</span>
                            }
                        </MudText>
                        <MudStack Row Spacing="1">
                            <MudButton StartIcon="@Icons.Material.Filled.ContentCopy" Size="Size.Small" Variant="Variant.Text"
                                Style="color: #7d8590;" OnClick="CopyConsoleOutput"
                                Disabled="@string.IsNullOrEmpty(ExamState.ConsoleOutput)">
                                Copy
                            </MudButton>
                        </MudStack>
                    </MudStack>
                </MudPaper>
            </MudPaper>
        </div>
    }

    <!-- CSV Viewer Overlay -->
    @if (_isCsvOverlayOpen)
    {
        <div
            style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 3500; display: flex; align-items: center; justify-content: center; pointer-events: none;">
            <MudPaper Class="pa-0"
                Style="width: 90vw; height: 80vh; max-width: 1400px; display: flex; flex-direction: column; pointer-events: auto;"
                Elevation="8">
                <!-- CSV Header -->
                <MudPaper Class="pa-3" Elevation="0"
                    Style="background-color: #1e1e1e; color: white; border-radius: 4px 4px 0 0;">
                    <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                        <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                            <MudIcon Icon="@Icons.Material.Filled.TableChart" />
                            <MudText Typo="Typo.h6">CSV Data Viewer</MudText>

                            <!-- File Name Chip -->
                            <MudChip T="string" Color="Color.Info" Size="Size.Small" Icon="@Icons.Material.Filled.Description"
                                Style="font-family: monospace;">
                                @_selectedCsvFile?.Name
                            </MudChip>
                        </MudStack>

                        <MudStack Row Spacing="1">
                            <MudIconButton Icon="@Icons.Material.Filled.ContentCopy" Color="Color.Inherit" Size="Size.Small"
                                OnClick="() => CopyPath(_selectedCsvFile?.Path ?? string.Empty)" />
                            <MudIconButton Icon="@Icons.Material.Filled.Close" Color="Color.Inherit" Size="Size.Small"
                                OnClick="CloseCsvOverlay" />
                        </MudStack>
                    </MudStack>
                </MudPaper>

                <!-- CSV Info Bar -->
                <MudPaper Class="pa-2" Elevation="0"
                    Style="background-color: #2d3748; color: #e2e8f0; border-bottom: 1px solid #4a5568;">
                    <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                        <MudStack Row AlignItems="AlignItems.Center" Spacing="3">
                            <MudText Typo="Typo.caption">
                                <MudIcon Icon="@Icons.Material.Filled.Folder" Size="Size.Small" Class="mr-1" />
                                Path: @_selectedCsvFile?.Path
                            </MudText>
                            @if (_csvData != null && _csvHeaders != null)
                            {
                                <MudText Typo="Typo.caption">
                                    <MudIcon Icon="@Icons.Material.Filled.GridOn" Size="Size.Small" Class="mr-1" />
                                    @_csvData.Count rows × @_csvHeaders.Count columns
                                </MudText>
                            }
                        </MudStack>
                        <MudText Typo="Typo.caption" Style="color: #a0aec0;">
                            CSV Data Preview
                        </MudText>
                    </MudStack>
                </MudPaper>

                <!-- CSV Content -->
                <div style="flex: 1; overflow: hidden; background-color: #f7fafc;">
                    @if (_selectedCsvFile != null && _csvData != null && _csvHeaders != null)
                    {
                        <MudDataGrid T="Dictionary<string, string>" Items="_csvData" Dense="false" Hover="true" Bordered="true"
                            Striped="true" FixedHeader="true" Height="100%" Virtualize="true" Style="height: 100%;">
                            <Columns>
                                <!-- Row Number Column -->
                                <TemplateColumn Title="#" Sortable="false" Filterable="false"
                                    Style="width: 60px; text-align: center; background-color: #edf2f7;">
                                    <CellTemplate>
                                        <MudText Typo="Typo.caption" Style="color: #718096; font-weight: bold;">
                                            @(_csvData.IndexOf(context.Item) + 1)
                                        </MudText>
                                    </CellTemplate>
                                </TemplateColumn>

                                <!-- Data Columns -->
                                @foreach (var header in _csvHeaders)
                                {
                                    <TemplateColumn Title="@header" Sortable="true" Filterable="true" Resizable="true">
                                        <HeaderTemplate>
                                            <MudStack Row AlignItems="AlignItems.Center" Spacing="1">
                                                <MudIcon Icon="@Icons.Material.Filled.TableChart" Size="Size.Small" />
                                                <MudText Typo="Typo.body2" Class="font-weight-bold">@header</MudText>
                                            </MudStack>
                                        </HeaderTemplate>
                                        <CellTemplate>
                                            <MudText Typo="Typo.body2" Style="word-break: break-word;">
                                                @(context.Item.ContainsKey(header) ? context.Item[header] : string.Empty)
                                            </MudText>
                                        </CellTemplate>
                                    </TemplateColumn>
                                }
                            </Columns>
                            <NoRecordsContent>
                                <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center" Style="height: 200px;">
                                    <MudIcon Icon="@Icons.Material.Filled.TableChart" Size="Size.Large" Style="color: #cbd5e0;" />
                                    <MudText Typo="Typo.h6" Style="color: #718096;">No data available</MudText>
                                    <MudText Typo="Typo.body2" Style="color: #a0aec0;">The CSV file appears to be empty</MudText>
                                </MudStack>
                            </NoRecordsContent>
                            <LoadingContent>
                                <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center" Style="height: 200px;">
                                    <MudProgressCircular Indeterminate="true" />
                                    <MudText Typo="Typo.body1">Loading CSV data...</MudText>
                                </MudStack>
                            </LoadingContent>
                        </MudDataGrid>
                    }
                    else
                    {
                        <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center" Style="height: 100%;">
                            <MudProgressCircular Indeterminate="true" Size="Size.Large" />
                            <MudText Typo="Typo.h6" Class="mt-4">Processing CSV data...</MudText>
                            <MudText Typo="Typo.body2" Style="color: #718096;">Please wait while we parse the file</MudText>
                        </MudStack>
                    }
                </div>

                <!-- CSV Footer -->
                <MudPaper Class="pa-2" Elevation="0"
                    Style="background-color: #2d3748; color: #e2e8f0; border-radius: 0 0 4px 4px;">
                    <MudStack Row Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                        <MudStack Row AlignItems="AlignItems.Center" Spacing="2">
                            @if (_csvData != null && _csvHeaders != null)
                            {
                                <MudText Typo="Typo.caption">
                                    <MudIcon Icon="@Icons.Material.Filled.Assessment" Size="Size.Small" Class="mr-1" />
                                    Data loaded successfully
                                </MudText>
                            }
                            else
                            {
                                <MudText Typo="Typo.caption">
                                    <MudIcon Icon="@Icons.Material.Filled.HourglassEmpty" Size="Size.Small" Class="mr-1" />
                                    Processing...
                                </MudText>
                            }
                        </MudStack>
                        <MudStack Row Spacing="1">
                            <MudButton StartIcon="@Icons.Material.Filled.ContentCopy" Size="Size.Small" Variant="Variant.Text"
                                Style="color: #e2e8f0;" OnClick="() => CopyPath(_selectedCsvFile?.Path ?? string.Empty)">
                                Copy Path
                            </MudButton>
                        </MudStack>
                    </MudStack>
                </MudPaper>
            </MudPaper>
        </div>
    }

    <!-- Background Overlay for Console -->
    <MudOverlay DarkBackground="true" AutoClose="true" OnClosed="CloseConsole" Visible="@_isConsoleOpen" />

    <!-- Background Overlay for CSV -->
    <MudOverlay DarkBackground="true" AutoClose="true" OnClosed="CloseCsvOverlay" Visible="@_isCsvOverlayOpen" />
}

@code {
     private bool showFullscreenWarning = true;
    private bool _isLoading = true;
    private bool _isConsoleOpen = false;
    private string _lastConsoleOutput = "";
    private bool _autoShowOnRun = false;

    // CSV Overlay variables
    private bool _isCsvOverlayOpen = false;
    private ExamFile? _selectedCsvFile = null;
    private List<Dictionary<string, string>>? _csvData = null;
    private List<string>? _csvHeaders = null;

    protected override async Task OnInitializedAsync()
    {
        ExamState.SetInvokeAsync(async (func) => await InvokeAsync(func));
        ExamState.OnChange += OnStateChanged;
        ExamState.OnCodeRunStarted += OnCodeRunStarted;
        ExamState.OnShowCsvOverlay += ShowCsvOverlay;

        try
        {
            _isLoading = true;
            StateHasChanged();

            var examData = await ApiClient.GenerateExamAsync();
            ExamState.LoadExam(examData.Exam, examData.Files);

            var mainFile = ExamState.GetFileByPath("src/Main.java");
            if (mainFile != null)
            {
                ExamState.OpenFile(mainFile);
            }

            _isLoading = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _isLoading = false;
            ExamState.SetConsoleOutput($"Error loading exam: {ex.Message}");
            StateHasChanged();
        }
    }

    private async Task RefreshPage()
    {
        await JSRuntime.InvokeVoidAsync("location.reload");
    }

    private void OnCodeRunStarted()
    {
        _autoShowOnRun = true;
    }

    private void OnStateChanged()
    {
        if (_autoShowOnRun &&
        !string.IsNullOrEmpty(ExamState.ConsoleOutput) &&
        ExamState.ConsoleOutput != _lastConsoleOutput &&
        !_isConsoleOpen)
        {
            _isConsoleOpen = true;
            _autoShowOnRun = false;
        }

        _lastConsoleOutput = ExamState.ConsoleOutput;
        StateHasChanged();
    }

    private void ToggleConsole()
    {
        _isConsoleOpen = !_isConsoleOpen;
        _autoShowOnRun = false;
    }

    private void CloseConsole()
    {
        _isConsoleOpen = false;
        _autoShowOnRun = false;
    }

    private void MinimizeConsole()
    {
        _isConsoleOpen = false;
        _autoShowOnRun = false;
    }

    private void ClearConsole()
    {
        ExamState.SetConsoleOutput("");
    }

    private async Task CopyConsoleOutput()
    {
        if (!string.IsNullOrEmpty(ExamState.ConsoleOutput))
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", ExamState.ConsoleOutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to copy console output: {ex.Message}");
            }
        }
    }

    // CSV Overlay methods
    public void ShowCsvOverlay(ExamFile csvFile)
    {
        _selectedCsvFile = csvFile;
        ParseCsvData(csvFile.Content);
        _isCsvOverlayOpen = true;
        StateHasChanged();
    }

    private void ParseCsvData(string csvContent)
    {
        try
        {
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            _csvHeaders = ParseCsvLine(lines[0]);
            _csvData = new List<Dictionary<string, string>>();
            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                var row = new Dictionary<string, string>();

                for (int j = 0; j < _csvHeaders.Count; j++)
                {
                    row[_csvHeaders[j]] = j < values.Count ? values[j] : string.Empty;
                }
                _csvData.Add(row);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing CSV: {ex.Message}");
            _csvData = new List<Dictionary<string, string>>();
            _csvHeaders = new List<string>();
        }
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result;
    }

    private void CloseCsvOverlay()
    {
        _isCsvOverlayOpen = false;
        _selectedCsvFile = null;
        _csvData = null;
        _csvHeaders = null;
        StateHasChanged();
    }

    private async Task CopyPath(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", path);
                Console.WriteLine($"Path copied to clipboard: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to copy path: {ex.Message}");
            }
        }
    }

    private Color GetTimerChipColor()
    {
        if (ExamState.TimeRemainingSeconds <= 60)
            return Color.Error;
        else if (ExamState.TimeRemainingSeconds <= 300)
            return Color.Warning;
        else
            return Color.Success;
    }

    private string FormatConsoleOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
            return "";

        var lines = output.Split('\n');
        var formattedLines = new List<string>();

        foreach (var line in lines)
        {
            var formattedLine = line;
            var lineColor = "#e6edf3";

            if (IsErrorLine(line))
            {
                lineColor = "#ff6b6b";
            }
            else if (IsWarningLine(line))
            {
                lineColor = "#ffa726";
            }
            else if (IsSuccessLine(line))
            {
                lineColor = "#66bb6a";
            }
            else if (IsInfoLine(line))
            {
                lineColor = "#42a5f5";
            }

            formattedLine = $"<span style=\"color: {lineColor};\">{System.Web.HttpUtility.HtmlEncode(line)}</span>";
            formattedLines.Add(formattedLine);
        }

        return string.Join("<br/>", formattedLines);
    }

    private bool IsErrorLine(string line)
    {
        var lowerLine = line.ToLower();
        return lowerLine.Contains("error") ||
        lowerLine.Contains("exception") ||
        lowerLine.Contains("failed") ||
        lowerLine.Contains("cannot") ||
        lowerLine.Contains("unable") ||
        lowerLine.Contains("invalid") ||
        lowerLine.Contains("compilation failed") ||
        lowerLine.Contains("build failed") ||
        lowerLine.StartsWith("at ") ||
        lowerLine.Contains("caused by:");
    }

    private bool IsWarningLine(string line)
    {
        var lowerLine = line.ToLower();
        return lowerLine.Contains("warning") ||
        lowerLine.Contains("deprecated") ||
        lowerLine.Contains("note:");
    }

    private bool IsSuccessLine(string line)
    {
        var lowerLine = line.ToLower();
        return lowerLine.Contains("success") ||
        lowerLine.Contains("completed") ||
        lowerLine.Contains("finished") ||
        lowerLine.Contains("build successful") ||
        lowerLine.Contains("compilation successful") ||
        lowerLine.StartsWith("=== running") ||
        lowerLine.Contains("submitted successfully");
    }

    private bool IsInfoLine(string line)
    {
        var lowerLine = line.ToLower();
        return lowerLine.StartsWith("info:") ||
        lowerLine.Contains("loading") ||
        lowerLine.Contains("starting") ||
        lowerLine.Contains("initializing");
    }

    private string FormatTime(int seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);
        if (timeSpan.TotalHours >= 1)
        {
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        else
        {
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }

    private async Task SubmitExam()
    {
        try
        {
            var files = ExamState.GetAllFiles();
            await ApiClient.SubmitExamAsync(files);
            ExamState.MarkAsSubmitted();
            ExamState.SetConsoleOutput("Exam submitted successfully!");
        }
        catch (Exception ex)
        {
            ExamState.SetConsoleOutput($"Submit failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        ExamState.OnChange -= OnStateChanged;
        ExamState.OnCodeRunStarted -= OnCodeRunStarted;
        ExamState.OnShowCsvOverlay -= ShowCsvOverlay;
    }
}
```
#### Layout folder
##### MainLayout.razor
```
@inherits LayoutComponentBase

<div class="page">
    <main>
        @Body
    </main>
</div>

<div id="blazor-error-ui">
    <environment include="Staging,Production">
        An error has occurred. This application may no longer respond until reloaded.
    </environment>
    <environment include="Development">
        An unhandled exception has occurred. See browser dev tools for details.
    </environment>
    <a href="" class="reload">Reload</a>
    <a class="dismiss">🗙</a>
</div>

<style>
    #blazor-error-ui {
        background: lightyellow;
        bottom: 0;
        box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
        display: none;
        left: 0;
        padding: 0.6rem 1.25rem 0.7rem 1.25rem;
        position: fixed;
        width: 100%;
        z-index: 1000;
    }

    #blazor-error-ui .dismiss {
        cursor: pointer;
        position: absolute;
        right: 0.75rem;
        top: 0.5rem;
    }
</style>
```
##### MainLayout.razor.css
```css
.page {
    position: relative;
    display: flex;
    flex-direction: column;
    min-height: 100vh;
}

main {
    flex: 1;
}
```
### Models folder
#### ApiModels.cs
```cs
namespace AIExamIDE.Models;

public class ExamResponse
{
    public ExamMetadata Exam { get; set; } = new();
    public List<ExamFile> Files { get; set; } = new();
}

public class RunResponse
{
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
}
```
#### ExamModels.cs
```cs
namespace AIExamIDE.Models;

public class ExamMetadata
{
    public string Domain { get; set; } = "";
    public string Overview { get; set; } = "";
    public List<ExamTask> Tasks { get; set; } = new();
    public int? Duration { get; set; } = 50; // Duration in minutes, default to 60
}

public class ExamTask
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public class ExamFile
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsDirectory { get; set; } = false;
    public List<ExamFile> Children { get; set; } = new();
}
```
### Services folder
#### ApiClient.cs
```cs
using AIExamIDE.Models;
using System.Text.Json;

namespace AIExamIDE.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ExamResponse> GenerateExamAsync()
    {
        var response = await _httpClient.PostAsync("/exam", null);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ExamResponse>(json, _jsonOptions)!;
    }

    public async Task<RunResponse> RunCodeAsync(List<ExamFile> files, string? mainFile = null)
    {
        var request = new { Files = files, MainFile = mainFile };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/run", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RunResponse>(responseJson, _jsonOptions)!;
    }

    public async Task ResetExamAsync()
    {
        var response = await _httpClient.PostAsync("/reset", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task SubmitExamAsync(List<ExamFile> files)
    {
        var request = new { Files = files };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/submit", content);
        response.EnsureSuccessStatusCode();
    }
}
```
#### ExamState.cs
```cs
using AIExamIDE.Models;
using System.Collections.Concurrent;

namespace AIExamIDE.Services
{
    public class ExamState : IDisposable
    {
        private readonly Timer _timer;
        private bool _disposed = false;
        private Func<Func<Task>, Task>? _invokeAsync;

        public ExamMetadata? CurrentExam { get; private set; }
        public List<ExamFile> Files { get; private set; } = new();
        public List<ExamFile> OpenFiles { get; private set; } = new();
        public ExamFile? ActiveFile { get; private set; }
        public int CurrentTaskIndex { get; set; } = 0;
        public int TimeRemainingSeconds { get; private set; } = 3000; 
        public bool IsSubmitted { get; private set; } = false;
        public string ConsoleOutput { get; private set; } = "Ready to run code...";

        public event Action? OnChange;

        public ExamState()
        {
            _timer = new Timer(TimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void SetInvokeAsync(Func<Func<Task>, Task> invokeAsync)
        {
            _invokeAsync = invokeAsync;
        }

        private async void TimerCallback(object? state)
        {
            if (_disposed) return;

            if (TimeRemainingSeconds > 0 && !IsSubmitted)
            {
                TimeRemainingSeconds--;
                
                if (_invokeAsync != null)
                {
                    try
                    {
                        await _invokeAsync(() =>
                        {
                            OnChange?.Invoke();
                            return Task.CompletedTask;
                        });
                    }
                    catch (ObjectDisposedException)
                    {
                        // Component has been disposed, stop the timer
                        return;
                    }
                }

                if (TimeRemainingSeconds <= 0)
                {
                    // Time's up - auto submit
                    MarkAsSubmitted();
                    SetConsoleOutput("⏰ Time's up! Exam automatically submitted.");
                }
            }
        }

        public void LoadExam(ExamMetadata exam, List<ExamFile> files)
        {
            if (_disposed) return;

            CurrentExam = exam;
            Files = files;
            // Set default time limit if not specified in exam metadata
            TimeRemainingSeconds = (exam.Duration ?? 60) * 60; // Convert minutes to seconds, default to 60 minutes
            CurrentTaskIndex = 0;
            NotifyStateChanged();
        }

        public void OpenFile(ExamFile file)
        {
            if (_disposed) return;

            if (!OpenFiles.Contains(file))
            {
                OpenFiles.Add(file);
            }
            ActiveFile = file;
            NotifyStateChanged();
        }

        public void CloseFile(ExamFile file)
        {
            if (_disposed) return;

            OpenFiles.Remove(file);
            if (ActiveFile == file)
            {
                ActiveFile = OpenFiles.LastOrDefault();
            }
            NotifyStateChanged();
        }

        public void SetActiveFile(ExamFile file)
        {
            if (_disposed) return;

            if (OpenFiles.Contains(file))
            {
                ActiveFile = file;
                NotifyStateChanged();
            }
        }

        public ExamFile? GetFileByPath(string path)
        {
            return Files.FirstOrDefault(f => f.Path == path);
        }

        public List<ExamFile> GetAllFiles()
        {
            return Files.ToList();
        }

        public List<ExamFile> GetRunnableFiles()
        {
            return Files.Where(f => !f.IsDirectory && HasMainMethod(f)).ToList();
        }

        public bool HasMainMethod(ExamFile file)
        {
            if (file.IsDirectory || !file.Name.EndsWith(".java"))
                return false;

            return file.Content.Contains("public static void main(String[]") ||
                   file.Content.Contains("public static void main(String []");
        }

        public event Action? OnConsoleOutput;
    
    public void SetConsoleOutput(string output)
    {
        ConsoleOutput = output;
        OnConsoleOutput?.Invoke();
        OnChange?.Invoke();
    }

        public void MarkAsSubmitted()
        {
            if (_disposed) return;

            IsSubmitted = true;
            NotifyStateChanged();
        }

         public event Action? OnCodeRunStarted;

    public void NotifyCodeRunStarted()
        {
            OnCodeRunStarted?.Invoke();
        }

        public void ShowCsvOverlay(ExamFile csvFile)
{
    OnShowCsvOverlay?.Invoke(csvFile);
}

public event Action<ExamFile>? OnShowCsvOverlay;

        public void AddTime(int seconds)
        {
            if (_disposed) return;

            TimeRemainingSeconds += seconds;
            NotifyStateChanged();
        }

        public void SetTime(int seconds)
        {
            if (_disposed) return;

            TimeRemainingSeconds = seconds;
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            if (_disposed) return;

            try
            {
                OnChange?.Invoke();
            }
            catch (ObjectDisposedException)
            {
                // Component has been disposed, ignore
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _timer?.Dispose();
                OnChange = null;
                Console.WriteLine("🔧 ExamState disposed - Timer stopped");
            }
        }
    }
}
```
### wwwroot folder
#### js folder
##### fullscreen.js
```js
window.fullscreenMonitor = {
    dotNetRef: null,
    
    initialize: function(dotNetRef) {
        this.dotNetRef = dotNetRef;
        
        // Check initial state
        this.checkFullscreenStatus();
        
        // Listen for fullscreen changes
        document.addEventListener('fullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('webkitfullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('mozfullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('MSFullscreenChange', () => this.handleFullscreenChange());
        
        // Prevent escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isFullscreen()) {
                e.preventDefault();
            }
        });
        
        // Monitor window focus
        window.addEventListener('blur', () => {
            setTimeout(() => this.checkFullscreenStatus(), 100);
        });
    },
    
    isFullscreen: function() {
        return !!(
            document.fullscreenElement ||
            document.webkitFullscreenElement ||
            document.mozFullScreenElement ||
            document.msFullscreenElement
        );
    },
    
    checkFullscreenStatus: function() {
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnFullscreenChanged', this.isFullscreen());
        }
    },
    
    handleFullscreenChange: function() {
        setTimeout(() => this.checkFullscreenStatus(), 100);
    },
    
    requestFullscreen: function() {
        const element = document.documentElement;
        
        if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.webkitRequestFullscreen) {
            element.webkitRequestFullscreen();
        } else if (element.mozRequestFullScreen) {
            element.mozRequestFullScreen();
        } else if (element.msRequestFullscreen) {
            element.msRequestFullscreen();
        }
    },
    
    dispose: function() {
        this.dotNetRef = null;
    }
};

window.initializeFullscreenMonitor = (dotNetRef) => {
    window.fullscreenMonitor.initialize(dotNetRef);
};

window.requestFullscreen = () => {
    window.fullscreenMonitor.requestFullscreen();
};

window.disposeFullscreenMonitor = () => {
    window.fullscreenMonitor.dispose();
};
```
## server folder
### server.js
```js
const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const { spawn, spawnSync } = require('child_process');
const axios = require('axios');
const crypto = require('crypto');

const app = express();
const PORT = process.env.PORT || 3000;

// Security Configuration
const ADMIN_SECRET_KEY = process.env.ADMIN_SECRET_KEY || crypto.randomBytes(32).toString('hex');
const SESSION_SECRET = process.env.SESSION_SECRET || crypto.randomBytes(32).toString('hex');

// Store generated exams temporarily (in production, use a database)
const examStore = new Map();
const sessionStore = new Map();

// OpenAI API Configuration
const OPENAI_API_KEY = 'sk-qkqZ3gGvM2T5Hgc8epZwZlzPbiFF645OBf4F65ZJ_vT3BlbkFJOGylOtHcgI62QcmUm_IEteDdf_1FcpJS1NtEywQbMA';
const OPENAI_API_URL = 'https://api.openai.com/v1/chat/completions';

// Middleware
app.use(cors());
app.use(express.json({ limit: '10mb' }));

// Security middleware for admin routes
function authenticateAdmin(req, res, next) {
    const authHeader = req.headers.authorization;
    const sessionToken = req.headers['x-session-token'];
    
    // Check for Bearer token or session token
    if (authHeader && authHeader.startsWith('Bearer ')) {
        const token = authHeader.substring(7);
        if (token === ADMIN_SECRET_KEY) {
            return next();
        }
    }
    
    // Check session token
    if (sessionToken && sessionStore.has(sessionToken)) {
        const session = sessionStore.get(sessionToken);
        if (session.expires > Date.now()) {
            return next();
        } else {
            sessionStore.delete(sessionToken);
        }
    }
    
    return res.status(401).json({ 
        error: 'Unauthorized access. Valid authentication required.',
        hint: 'Use /admin/login endpoint to get session token'
    });
}

// Rate limiting for admin endpoints
const adminRateLimit = new Map();
function rateLimitAdmin(req, res, next) {
    const ip = req.ip || req.connection.remoteAddress;
    const now = Date.now();
    const windowMs = 15 * 60 * 1000; // 15 minutes
    const maxAttempts = 10;
    
    if (!adminRateLimit.has(ip)) {
        adminRateLimit.set(ip, { count: 1, resetTime: now + windowMs });
        return next();
    }
    
    const rateData = adminRateLimit.get(ip);
    if (now > rateData.resetTime) {
        rateData.count = 1;
        rateData.resetTime = now + windowMs;
        return next();
    }
    
    if (rateData.count >= maxAttempts) {
        return res.status(429).json({ 
            error: 'Too many requests. Please try again later.',
            retryAfter: Math.ceil((rateData.resetTime - now) / 1000)
        });
    }
    
    rateData.count++;
    next();
}

// Directories
const WORKSPACE_DIR = path.join(__dirname, 'workspace');
const SUBMISSIONS_DIR = path.join(__dirname, 'submissions');

// Ensure directories exist
fs.mkdirSync(WORKSPACE_DIR, { recursive: true });
fs.mkdirSync(path.join(WORKSPACE_DIR, 'src'), { recursive: true });
fs.mkdirSync(path.join(WORKSPACE_DIR, 'data'), { recursive: true });
fs.mkdirSync(SUBMISSIONS_DIR, { recursive: true });

// Enhanced GPT-4o Prompt for multiple CSV files with relationships
const EXAM_GENERATION_PROMPT = `Generate a Java programming exam in STRICT JSON format. You must return ONLY valid JSON with no extra text.

CRITICAL: The JSON must be perfectly formatted with proper quotes, commas, and brackets. No trailing commas allowed.

Required JSON structure:
{
  "exam": {
    "domain": "string",
    "csv_files": [
      {
        "filename": "string",
        "content": [
          {"field1": "value1", "field2": "value2"}
        ]
      }
    ],
    "tasks": [
      {"id": 1, "description": "string"},
      {"id": 2, "description": "string"},
      {"id": 3, "description": "string"},
      {"id": 4, "description": "string"}
    ],
    "overview": "string"
  }
}

Requirements:
1. Domain: Choose ONE unique domain from: "Aerospace Manufacturing", "Smart City Infrastructure", "Precision Agriculture", "Autonomous Vehicle Systems", "Quantum Computing Research", "Biotechnology R&D", "Renewable Energy Grid Management", "Supply Chain Optimization", "Robotics Process Automation", "Cybersecurity Threat Intelligence", "Satellite Communication Networks", "Genomic Data Analysis", "Augmented Reality Training", "Industrial IoT Solutions", "Financial Algorithmic Trading", "Pharmaceutical Drug Discovery", "Maritime Logistics", "Waste Management Solutions", "Geospatial Intelligence", "Elderly Care Monitoring", "Legal Tech Platforms", "Environmental Impact Assessment", "Digital Forensics", "Space Exploration Systems", "Microgrid Energy Management", "AI-Powered Personalization", "Blockchain Supply Chain Tracking", "Virtual Reality Healthcare", "Edge Computing Networks", "Sustainable Urban Planning", "Neuroscience Research Platforms", "Advanced Materials Science", "Predictive Maintenance Solutions", "Humanoid Robotics Development", "Personalized Medicine Delivery", "Deep Learning for Drug Discovery", "Cognitive Computing Systems", "Digital Twin Technology", "Hyper-automation Services", "Ethical AI Development", "Carbon Capture Technologies", "Oceanic Data Analytics", "Asteroid Mining Operations", "Bioinformatics for Gene Editing", "Quantum Cryptography", "Swarm Robotics Applications", "Precision Fermentation", "Exoskeleton Technology", "Haptic Feedback Systems", "Neuromorphic Computing", "AI Ethics & Governance", "Explainable AI (XAI)", "Federated Learning Platforms", "Data Observability Solutions", "MLOps Platforms", "Natural Language Generation (NLG)", "Computer Vision for Quality Control", "Predictive Analytics for HR", "Anomaly Detection Systems", "Recommendation Engine Development", "Decentralized Finance (DeFi)", "Non-Fungible Tokens (NFTs) Management", "Digital Identity Solutions (Blockchain)", "Metaverse Development", "Tokenization of Real-World Assets", "Personalized Nutrition Planning", "Digital Therapeutics", "Remote Patient Monitoring", "Medical Imaging Analysis", "Gene Therapy Development", "Regenerative Medicine", "Telemedicine Platforms", "Clinical Trial Management Systems", "Autonomous Drones for Inspection", "Collaborative Robotics (Cobots)", "Automated Warehousing Systems", "Surgical Robotics", "Satellite Data Analytics", "Space Debris Tracking", "Hypersonic Technology Development", "Defense AI Systems", "Planetary Resource Exploration", "Carbon Footprint Tracking", "Waste-to-Energy Solutions", "Water Resource Management", "Environmental Monitoring Sensors", "Geothermal Energy Systems", "Ocean Cleanup Technologies", "Additive Manufacturing (3D Printing)", "Industrial Cybersecurity", "Smart Factory Solutions", "Quality Control Automation", "Adaptive Learning Platforms", "Gamified Education", "Vocational Training Simulators", "Corporate Learning Management", "Skill-based Credentialing", "RegTech (Regulatory Technology)", "InsurTech (Insurance Technology)", "Legal Document Automation"

2. CSV Files: Create either 1 or 2 CSV files (randomly choose):
   - If 1 CSV: Create 15 rows of realistic data
   - If 2 CSVs: Create a main entity CSV (12-15 rows) and a related entity CSV (18-25 rows) with a foreign key relationship
   
   Examples of relationships:
   - customers.csv (CustomerID) + orders.csv (CustomerID, OrderID)
   - products.csv (ProductID) + reviews.csv (ProductID, ReviewID)
   - students.csv (StudentID) + enrollments.csv (StudentID, CourseID)
   - employees.csv (EmployeeID) + projects.csv (EmployeeID, ProjectID)

3. Tasks: Create exactly 4 detailed task descriptions (each 4-6 sentences). Tasks must:
   - Be comprehensive and detailed without giving implementation hints
   - Require working with data from both CSV files if there are two
   - Mention working ONLY in the src/ folder for file operations
   - Include complex data analysis and relationships
   - CRITICAL: ALL OUTPUT MUST BE DISPLAYED ON SCREEN ONLY - NO FILE SAVING OF PARSED DATA
   - NEVER ask students to save parsed data, analysis results, or reports to files
   - ALL results, reports, and analysis must be printed to console/screen only
   - Task 1: Load and filter data with complex conditions across files - DISPLAY results on screen
   - Task 2: Analyze relationships and DISPLAY comprehensive reports on screen only
   - Task 3: Implement advanced sorting with multiple criteria and error handling - PRINT results
   - Task 4: Create complex data structures and statistical analysis - SHOW results on screen

4. Overview: 3-4 sentences explaining the comprehensive business context.

IMPORTANT: Return ONLY the JSON object. No markdown, no code blocks, no extra text. Ensure all strings are properly quoted and no trailing commas exist.`;

// Function to clean and fix JSON
function cleanAndFixJSON(jsonString) {
    try {
        // Remove any markdown formatting
        let cleaned = jsonString.replace(/```json\s*/g, '').replace(/```\s*/g, '').trim();
        
        // Remove any text before the first {
        const firstBrace = cleaned.indexOf('{');
        if (firstBrace > 0) {
            cleaned = cleaned.substring(firstBrace);
        }
        
        // Remove any text after the last }
        const lastBrace = cleaned.lastIndexOf('}');
        if (lastBrace >= 0) {
            cleaned = cleaned.substring(0, lastBrace + 1);
        }
        
        // Fix common JSON issues
        cleaned = cleaned
            // Remove trailing commas before closing brackets/braces
            .replace(/,(\s*[}\]])/g, '$1')
            // Fix missing commas between objects
            .replace(/}(\s*){/g, '},$1{')
            // Fix missing commas between array elements
            .replace(/}(\s*)\]/g, '},$1]')
            // Basic string value fixing (simplified)
            .replace(/:\s*([a-zA-Z0-9_]+)([,}\]])/g, ': "$1"$2');
        
        return cleaned;
    } catch (error) {
        console.error('Error cleaning JSON:', error.message);
        return jsonString;
    }
}

// Function to call GPT-4o API with better error handling
async function generateExamWithGPT() {
    try {
        console.log('🤖 Generating NEW exam with GPT-4o...');
        
        const response = await axios.post(OPENAI_API_URL, {
            model: 'gpt-4o',
            messages: [
                {
                    role: 'system',
                    content: 'You are a JSON generator. You must respond with ONLY valid JSON. No markdown, no explanations, no code blocks. Just pure, valid JSON that can be parsed directly.'
                },
                {
                    role: 'user',
                    content: EXAM_GENERATION_PROMPT
                }
            ],
            max_tokens: 8000,
            temperature: 0.8,
            response_format: { type: "json_object" } // Force JSON response
        }, {
            headers: {
                'Authorization': `Bearer ${OPENAI_API_KEY}`,
                'Content-Type': 'application/json'
            }
        });

        let gptResponse = response.data.choices[0].message.content.trim();
        console.log('📝 GPT-4o response received');
        console.log('Raw response length:', gptResponse.length);
        
        // Clean the JSON
        const cleanedResponse = cleanAndFixJSON(gptResponse);
        console.log('Cleaned response preview:', cleanedResponse.substring(0, 200) + '...');
        
        // Try to parse the JSON
        let examData;
        try {
            examData = JSON.parse(cleanedResponse);
        } catch (parseError) {
            console.error('First parse attempt failed:', parseError.message);
            
            // Try alternative cleaning approach
            const alternativeCleaned = gptResponse
                .replace(/```json/g, '')
                .replace(/```/g, '')
                .replace(/,(\s*[}\]])/g, '$1') // Remove trailing commas
                .trim();
            
            console.log('Trying alternative cleaning...');
            examData = JSON.parse(alternativeCleaned);
        }
        
        // Validate the structure
        if (!examData.exam || !examData.exam.domain || !examData.exam.csv_files || !examData.exam.tasks) {
            throw new Error('Invalid exam structure received from GPT');
        }
        
        console.log('✅ NEW Exam data parsed successfully');
        console.log('🎯 Domain:', examData.exam.domain);
        console.log('📊 CSV files:', examData.exam.csv_files.length);
        console.log('📋 Tasks:', examData.exam.tasks.length);
        
        return examData;
    } catch (error) {
        console.error('❌ Error generating exam with GPT-4o:', error.message);
        if (error.response) {
            console.error('API Error Status:', error.response.status);
            console.error('API Error Data:', error.response.data);
        }
        
        // Fallback to random sample exam
        console.log('🔄 Falling back to random sample exam...');
        return getRandomSampleExam();
    }
}

// Enhanced sample exams with relational data - UPDATED TO REMOVE FILE SAVING
function getRandomSampleExam() {
    const sampleExams = [
        {
            exam: {
                domain: "E-commerce Platform Management",
                csv_files: [
                    {
                        filename: "customers.csv",
                        content: [
                            { "CustomerID": "C001", "Name": "John Smith", "Email": "john.smith@email.com", "Phone": "555-0101", "City": "New York", "RegistrationDate": "2023-01-15", "Status": "Premium" },
                            { "CustomerID": "C002", "Name": "Sarah Johnson", "Email": "sarah.j@email.com", "Phone": "555-0102", "City": "Los Angeles", "RegistrationDate": "2023-02-20", "Status": "Regular" },
                            { "CustomerID": "C003", "Name": "Mike Wilson", "Email": "mike.w@email.com", "Phone": "555-0103", "City": "Chicago", "RegistrationDate": "2023-01-10", "Status": "Premium" },
                            { "CustomerID": "C004", "Name": "Emma Davis", "Email": "emma.d@email.com", "Phone": "555-0104", "City": "Houston", "RegistrationDate": "2023-03-05", "Status": "Regular" },
                            { "CustomerID": "C005", "Name": "David Brown", "Email": "david.b@email.com", "Phone": "555-0105", "City": "Phoenix", "RegistrationDate": "2023-01-25", "Status": "VIP" },
                            { "CustomerID": "C006", "Name": "Lisa Garcia", "Email": "lisa.g@email.com", "Phone": "555-0106", "City": "Philadelphia", "RegistrationDate": "2023-02-14", "Status": "Regular" },
                            { "CustomerID": "C007", "Name": "Tom Anderson", "Email": "tom.a@email.com", "Phone": "555-0107", "City": "San Antonio", "RegistrationDate": "2023-01-30", "Status": "Premium" },
                            { "CustomerID": "C008", "Name": "Anna Lee", "Email": "anna.l@email.com", "Phone": "555-0108", "City": "San Diego", "RegistrationDate": "2023-03-12", "Status": "Regular" },
                            { "CustomerID": "C009", "Name": "Chris Martin", "Email": "chris.m@email.com", "Phone": "555-0109", "City": "Dallas", "RegistrationDate": "2023-02-08", "Status": "Premium" },
                            { "CustomerID": "C010", "Name": "Sophie Chen", "Email": "sophie.c@email.com", "Phone": "555-0110", "City": "San Jose", "RegistrationDate": "2023-01-18", "Status": "VIP" },
                            { "CustomerID": "C011", "Name": "Kevin Zhang", "Email": "kevin.z@email.com", "Phone": "555-0111", "City": "Austin", "RegistrationDate": "2023-02-25", "Status": "Regular" },
                            { "CustomerID": "C012", "Name": "Maria Rodriguez", "Email": "maria.r@email.com", "Phone": "555-0112", "City": "Jacksonville", "RegistrationDate": "2023-01-22", "Status": "Premium" }
                        ]
                    },
                    {
                        filename: "orders.csv",
                        content: [
                            { "OrderID": "O001", "CustomerID": "C001", "ProductName": "Laptop Pro", "Quantity": "1", "Price": "1299.99", "OrderDate": "2023-03-15", "Status": "Delivered" },
                            { "OrderID": "O002", "CustomerID": "C001", "ProductName": "Wireless Mouse", "Quantity": "2", "Price": "49.99", "OrderDate": "2023-03-20", "Status": "Delivered" },
                            { "OrderID": "O003", "CustomerID": "C002", "ProductName": "Smartphone", "Quantity": "1", "Price": "799.99", "OrderDate": "2023-03-18", "Status": "Shipped" },
                            { "OrderID": "O004", "CustomerID": "C003", "ProductName": "Tablet", "Quantity": "1", "Price": "599.99", "OrderDate": "2023-03-22", "Status": "Delivered" },
                            { "OrderID": "O005", "CustomerID": "C003", "ProductName": "Keyboard", "Quantity": "1", "Price": "129.99", "OrderDate": "2023-03-25", "Status": "Processing" },
                            { "OrderID": "O006", "CustomerID": "C004", "ProductName": "Monitor", "Quantity": "2", "Price": "299.99", "OrderDate": "2023-03-19", "Status": "Delivered" },
                            { "OrderID": "O007", "CustomerID": "C005", "ProductName": "Gaming Chair", "Quantity": "1", "Price": "399.99", "OrderDate": "2023-03-21", "Status": "Delivered" },
                            { "OrderID": "O008", "CustomerID": "C005", "ProductName": "Desk Lamp", "Quantity": "3", "Price": "79.99", "OrderDate": "2023-03-23", "Status": "Shipped" },
                            { "OrderID": "O009", "CustomerID": "C006", "ProductName": "Headphones", "Quantity": "1", "Price": "199.99", "OrderDate": "2023-03-17", "Status": "Delivered" },
                            { "OrderID": "O010", "CustomerID": "C007", "ProductName": "Webcam", "Quantity": "1", "Price": "89.99", "OrderDate": "2023-03-24", "Status": "Processing" },
                            { "OrderID": "O011", "CustomerID": "C008", "ProductName": "Printer", "Quantity": "1", "Price": "249.99", "OrderDate": "2023-03-16", "Status": "Delivered" },
                            { "OrderID": "O012", "CustomerID": "C009", "ProductName": "External Drive", "Quantity": "2", "Price": "119.99", "OrderDate": "2023-03-26", "Status": "Shipped" },
                            { "OrderID": "O013", "CustomerID": "C010", "ProductName": "Smart Watch", "Quantity": "1", "Price": "349.99", "OrderDate": "2023-03-14", "Status": "Delivered" },
                            { "OrderID": "O014", "CustomerID": "C010", "ProductName": "Phone Case", "Quantity": "4", "Price": "29.99", "OrderDate": "2023-03-27", "Status": "Processing" },
                            { "OrderID": "O015", "CustomerID": "C011", "ProductName": "Bluetooth Speaker", "Quantity": "1", "Price": "159.99", "OrderDate": "2023-03-13", "Status": "Delivered" },
                            { "OrderID": "O016", "CustomerID": "C012", "ProductName": "Laptop Stand", "Quantity": "1", "Price": "69.99", "OrderDate": "2023-03-28", "Status": "Shipped" },
                            { "OrderID": "O017", "CustomerID": "C001", "ProductName": "Cable Organizer", "Quantity": "5", "Price": "19.99", "OrderDate": "2023-03-29", "Status": "Processing" },
                            { "OrderID": "O018", "CustomerID": "C003", "ProductName": "Power Bank", "Quantity": "2", "Price": "89.99", "OrderDate": "2023-03-30", "Status": "Delivered" },
                            { "OrderID": "O019", "CustomerID": "C005", "ProductName": "USB Hub", "Quantity": "1", "Price": "39.99", "OrderDate": "2023-03-31", "Status": "Shipped" },
                            { "OrderID": "O020", "CustomerID": "C007", "ProductName": "Screen Protector", "Quantity": "3", "Price": "24.99", "OrderDate": "2023-04-01", "Status": "Processing" }
                        ]
                    }
                ],
                tasks: [
                    { 
                        "id": 1, 
                        "description": "Develop a comprehensive customer analysis system that loads data from both customers.csv and orders.csv files to identify high-value customers and their purchasing patterns. Your analysis should filter customers who have Premium or VIP status and have placed orders totaling more than $500 in value across all their transactions. For each qualifying customer, calculate their total order value, average order amount, number of orders placed, and most frequently purchased product category. Display this information in a detailed tabular format on the screen showing customer details alongside their comprehensive purchasing statistics. The system should also identify which customers have the highest individual order values and print any customers who might be considered for loyalty program upgrades based on their spending patterns." 
                    },
                    { 
                        "id": 2, 
                        "description": "Create an advanced order fulfillment and customer relationship management analysis that examines the correlation between customer status levels and their order behaviors across both data files. Generate a comprehensive business intelligence report that analyzes order distribution patterns, identifies customers with multiple orders, calculates average order processing times by customer tier, and determines which customer segments generate the most revenue per transaction. Your analysis should include statistical breakdowns of order statuses by customer type, identification of repeat customers and their loyalty metrics, and recommendations for inventory management based on popular product combinations. Display this detailed analytical report on the screen only, ensuring the output includes executive summary sections, detailed customer profiles, and actionable business insights for management decision-making." 
                    },
                    { 
                        "id": 3, 
                        "description": "Implement a sophisticated multi-dimensional sorting and data validation system that organizes the combined customer and order data using complex hierarchical criteria. Your sorting algorithm should first arrange customers by their status level (VIP, Premium, Regular), then within each status group by their total order value in descending order, and finally by registration date for customers with similar spending patterns. The system must include comprehensive error handling for data inconsistencies such as missing customer records for existing orders, invalid date formats, negative quantities or prices, and duplicate order IDs. Additionally, implement data integrity checks that verify all orders have corresponding customer records and flag any anomalies in the customer-order relationships. Print the sorted results on the screen with clear indicators of any data quality issues discovered during processing, and display detailed error logs for any problematic records encountered." 
                    },
                    { 
                        "id": 4, 
                        "description": "Design and implement a comprehensive e-commerce analytics dashboard using advanced Java collections that creates multiple interconnected data structures for deep business analysis. Construct a Map<String, List<Customer>> that groups customers by city for geographic analysis, a Map<String, List<Order>> that organizes orders by status for operational insights, and a Map<String, CustomerOrderSummary> that links each customer to their complete order history and calculated metrics. Your dashboard should perform complex statistical calculations including customer lifetime value analysis, geographic revenue distribution, product popularity rankings, seasonal ordering patterns, and customer retention rates. Display detailed analytics reports on the screen showing top-performing cities by revenue, most valuable customer segments, order fulfillment efficiency metrics, and predictive insights for inventory planning. The system should also identify cross-selling opportunities by analyzing customer purchase patterns and print recommendations for targeted marketing campaigns based on customer behavior analysis and geographic trends." 
                    }
                ],
                overview: "You are developing a comprehensive e-commerce platform management system for a growing online retail company that needs to analyze customer behavior, order patterns, and business performance across multiple dimensions. The system must provide detailed insights into customer segmentation, order fulfillment efficiency, revenue optimization, and strategic business intelligence to support data-driven decision making. This platform serves as the central analytics hub for understanding customer relationships, optimizing inventory management, and identifying growth opportunities in the competitive e-commerce marketplace."
            }
        }
    ];
    
    const randomIndex = Math.floor(Math.random() * sampleExams.length);
    const selectedExam = sampleExams[randomIndex];
    console.log(`🎲 Using random sample exam ${randomIndex + 1}: ${selectedExam.exam.domain}`);
    return selectedExam;
}

// Rest of the code remains the same...
function convertExamToFiles(examData) {
    const files = [];
    
    examData.exam.csv_files.forEach(csvFile => {
        if (csvFile.content && csvFile.content.length > 0) {
            const headers = Object.keys(csvFile.content[0]);
            let csvContent = headers.join(',') + '\n';
            csvFile.content.forEach(row => {
                const values = headers.map(header => row[header] || '');
                csvContent += values.join(',') + '\n';
            });
            
            // Add CSV file to data directory
            files.push({
                name: csvFile.filename,
                path: `data/${csvFile.filename}`,
                content: csvContent.trim(),
                isDirectory: false
            });
            
            // ALSO add CSV file to src directory for easier access
            files.push({
                name: csvFile.filename,
                path: `src/${csvFile.filename}`,
                content: csvContent.trim(),
                isDirectory: false
            });
        }
    });
    
    const mainJava = `import java.io.*;
import java.util.*;
import java.util.stream.Collectors;

/**
 * ${examData.exam.domain}
 * ${examData.exam.overview}
 */
public class Main {
    public static void main(String[] args) {
        System.out.println("=== ${examData.exam.domain} ===\\n");
        
        // TODO: Implement the following tasks:
        
        // Task 1: ${examData.exam.tasks[0].description.substring(0, 100)}...
        
        // Task 2: ${examData.exam.tasks[1].description.substring(0, 100)}...
        
        // Task 3: ${examData.exam.tasks[2].description.substring(0, 100)}...
        
        // Task 4: ${examData.exam.tasks[3].description.substring(0, 100)}...
        
        System.out.println("Please implement the required tasks!");
        System.out.println("Check the Tasks panel for detailed requirements.");
        System.out.println("Remember to work ONLY in the src/ folder for any file operations.");
        System.out.println("CSV files are available in the same directory as this file.");
        System.out.println("\\n*** IMPORTANT: Display ALL results on screen only - DO NOT save parsed data to files ***");
    }
}`;

    files.push({
        name: 'Main.java',
        path: 'src/Main.java',
        content: mainJava,
        isDirectory: false
    });
    
    return files;
}

function runCommand(cmd, args, options = {}) {
    try {
        console.log(`Running command: ${cmd} ${args.join(' ')}`);
        const result = spawnSync(cmd, args, { 
            encoding: 'utf8',
            timeout: 30000,
            ...options 
        });
        const stdout = result.stdout || '';
        const stderr = result.stderr || '';
        console.log(`Command result - Code: ${result.status}, Stdout: ${stdout.substring(0, 200)}, Stderr: ${stderr.substring(0, 200)}`);
        return { stdout, stderr, code: result.status ?? 0 };
    } catch (err) {
        console.log(`Command error: ${err.message}`);
        return { stdout: '', stderr: err.message, code: -1 };
    }
}

function writeFilesToWorkspace(files) {
    files.forEach(file => {
        if (!file.isDirectory) {
            const fullPath = path.join(WORKSPACE_DIR, file.path);
            const dir = path.dirname(fullPath);
            fs.mkdirSync(dir, { recursive: true });
            fs.writeFileSync(fullPath, file.content, 'utf8');
        }
    });
}

async function compileAndRun(mainClassName = 'Main') {
    const srcDir = path.join(WORKSPACE_DIR, 'src');
    const workspaceDir = WORKSPACE_DIR;
    
    console.log(`Starting Java compilation and execution for class: ${mainClassName}`);
    console.log('Source directory:', srcDir);
    console.log('Working directory:', workspaceDir);
    
    try {
        const javaCheck = runCommand('java', ['-version']);
        const javacCheck = runCommand('javac', ['-version']);
        
        if (javaCheck.code === 0 && javacCheck.code === 0) {
            console.log('Using local/container Java for compilation');
            
            const compileResult = runCommand('javac', ['-cp', '.', '*.java'], {
                cwd: srcDir
            });
            
            if (compileResult.code !== 0) {
                return {
                    output: '',
                    error: `Compilation failed:\n${compileResult.stderr}`
                };
            }
            
            // Run from workspace root so data/ directory is accessible
            const runResult = runCommand('java', ['-cp', 'src', mainClassName], {
                cwd: workspaceDir
            });
            
            return {
                output: runResult.stdout,
                error: runResult.stderr
            };
        } else {
            return {
                output: '',
                error: 'Java is not available. Please install Java JDK 17+ or use Docker.'
            };
        }
        
    } catch (error) {
        console.error('Error in compileAndRun:', error);
        return {
            output: '',
            error: `Execution failed: ${error.message}`
        };
    }
}

// SECURITY ROUTES

// Admin login endpoint
app.post('/admin/login', rateLimitAdmin, (req, res) => {
    const { password } = req.body;
    
    if (!password) {
        return res.status(400).json({ error: 'Password required' });
    }
    
    // Simple password check (in production, use proper hashing)
    const expectedPassword = process.env.ADMIN_PASSWORD || 'admin123!@#';
    
    if (password === expectedPassword) {
        // Generate session token
        const sessionToken = crypto.randomBytes(32).toString('hex');
        const expiresIn = 24 * 60 * 60 * 1000; // 24 hours
        
        sessionStore.set(sessionToken, {
            created: Date.now(),
            expires: Date.now() + expiresIn
        });
        
        console.log(`🔐 Admin login successful from ${req.ip}`);
        
        res.json({
            success: true,
            sessionToken,
            expiresIn,
            message: 'Authentication successful'
        });
    } else {
        console.warn(`🚫 Failed admin login attempt from ${req.ip}`);
        res.status(401).json({ error: 'Invalid credentials' });
    }
});

// Admin logout endpoint
app.post('/admin/logout', authenticateAdmin, (req, res) => {
    const sessionToken = req.headers['x-session-token'];
    
    if (sessionToken && sessionStore.has(sessionToken)) {
        sessionStore.delete(sessionToken);
    }
    
    res.json({ success: true, message: 'Logged out successfully' });
});

// Secure endpoint to retrieve exam JSON
app.get('/admin/exam/:examId', authenticateAdmin, (req, res) => {
    try {
        const { examId } = req.params;
        const { format } = req.query;
        
        if (!examStore.has(examId)) {
            return res.status(404).json({ 
                error: 'Exam not found',
                examId,
                availableExams: Array.from(examStore.keys())
            });
        }
        
        const examData = examStore.get(examId);
        
        // Log access
        console.log(`📋 Admin retrieved exam ${examId} from ${req.ip}`);
        
        if (format === 'download') {
            // Send as downloadable file
            res.setHeader('Content-Disposition', `attachment; filename="exam-${examId}.json"`);
            res.setHeader('Content-Type', 'application/json');
            return res.send(JSON.stringify(examData, null, 2));
        }
        
        // Send as JSON response
        res.json({
            success: true,
            examId,
            timestamp: examData.timestamp,
            exam: examData.exam,
            metadata: {
                domain: examData.exam.domain,
                csvFiles: examData.exam.csv_files.length,
                tasks: examData.exam.tasks.length,
                generated: examData.timestamp
            }
        });
        
    } catch (error) {
        console.error('Error retrieving exam:', error);
        res.status(500).json({ error: 'Failed to retrieve exam data' });
    }
});

// List all available exams
app.get('/admin/exams', authenticateAdmin, (req, res) => {
    try {
        const exams = Array.from(examStore.entries()).map(([id, data]) => ({
            examId: id,
            domain: data.exam.domain,
            csvFiles: data.exam.csv_files.length,
            tasks: data.exam.tasks.length,
            timestamp: data.timestamp,
            age: Date.now() - new Date(data.timestamp).getTime()
        }));
        
        res.json({
            success: true,
            count: exams.length,
            exams: exams.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))
        });
        
    } catch (error) {
        console.error('Error listing exams:', error);
        res.status(500).json({ error: 'Failed to list exams' });
    }
});

// Get exam tasks only (without CSV data)
app.get('/admin/exam/:examId/tasks', authenticateAdmin, (req, res) => {
    try {
        const { examId } = req.params;
        
        if (!examStore.has(examId)) {
            return res.status(404).json({ error: 'Exam not found' });
        }
        
        const examData = examStore.get(examId);
        
        res.json({
            success: true,
            examId,
            domain: examData.exam.domain,
            overview: examData.exam.overview,
            tasks: examData.exam.tasks,
            timestamp: examData.timestamp
        });
        
    } catch (error) {
        console.error('Error retrieving exam tasks:', error);
        res.status(500).json({ error: 'Failed to retrieve exam tasks' });
    }
});

// EXISTING ROUTES (updated to store exams)

// Routes
app.post('/exam', async (req, res) => {
    try {
        console.log('🚀 Starting FRESH exam generation...');
        console.log('🔄 No caching - generating completely new exam');
        
        const examData = await generateExamWithGPT();
        const files = convertExamToFiles(examData);
        
        writeFilesToWorkspace(files);
        console.log('📁 NEW exam files written to workspace');
        
        // Store exam with unique ID for admin retrieval
        const examId = crypto.randomBytes(16).toString('hex');
        examStore.set(examId, {
            exam: examData.exam,
            timestamp: new Date().toISOString(),
            files: files
        });
        
        // Clean up old exams (keep last 50)
        if (examStore.size > 50) {
            const oldestKey = examStore.keys().next().value;
            examStore.delete(oldestKey);
        }
        
        console.log(`💾 Exam stored with ID: ${examId}`);
        
        res.json({
            exam: examData.exam,
            files: files,
            examId: examId // Include exam ID in response for admin reference
        });
    } catch (error) {
        console.error('❌ Error generating exam:', error);
        res.status(500).json({ error: 'Failed to generate exam' });
    }
});

app.post('/run', async (req, res) => {
    try {
        const { files, mainFile } = req.body;
        
        if (!files) {
            return res.status(400).json({ error: 'No files provided' });
        }
        
        console.log('Received files for execution:', files.map(f => f.path));
        console.log('Main file specified:', mainFile);
        
        writeFilesToWorkspace(files);
        
        let fileToRun = 'Main';
        
        if (mainFile) {
            const mainFilePath = mainFile.replace(/^src\//, '').replace(/\.java$/, '');
            fileToRun = mainFilePath;
            console.log(`Running specified file: ${fileToRun}`);
        } else {
            const javaFiles = files.filter(f => f.path.endsWith('.java') && !f.isDirectory);
            
            for (const file of javaFiles) {
                console.log(`Checking file: ${file.path}`);
                const hasMainMethod = file.content.includes('public static void main(String');
                console.log(`  - Has main method: ${hasMainMethod}`);
                
                if (hasMainMethod) {
                    fileToRun = file.path.replace(/^src\//, '').replace(/\.java$/, '');
                    console.log(`✅ Auto-detected main file: ${fileToRun}`);
                    break;
                }
            }
        }
        
        const result = await compileAndRun(fileToRun);
        console.log('Execution result:', result);
        res.json(result);
    } catch (error) {
        console.error('Error running code:', error);
        res.status(500).json({ error: 'Failed to run code' });
    }
});

app.post('/reset', (req, res) => {
    try {
        console.log('⚠️  Reset not supported - generate new exam instead');
        res.json({ 
            success: false, 
            message: 'Reset not supported. Refresh page to generate new exam.' 
        });
    } catch (error) {
        console.error('Error resetting exam:', error);
        res.status(500).json({ error: 'Failed to reset exam' });
    }
});

app.post('/submit', (req, res) => {
    try {
        const { files } = req.body;
        
        if (!files) {
            return res.status(400).json({ error: 'No files provided' });
        }
        
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const submissionDir = path.join(SUBMISSIONS_DIR, `submission-${timestamp}`);
        
        fs.mkdirSync(submissionDir, { recursive: true });
        
        files.forEach(file => {
            if (!file.isDirectory) {
                const filePath = path.join(submissionDir, file.name);
                fs.writeFileSync(filePath, file.content, 'utf8');
            }
        });
        
        console.log(`Exam submitted and saved to: ${submissionDir}`);
        res.json({ success: true, submissionId: timestamp });
    } catch (error) {
        console.error('Error submitting exam:', error);
        res.status(500).json({ error: 'Failed to submit exam' });
    }
});

app.get('/health', (req, res) => {
    res.json({ status: 'OK', timestamp: new Date().toISOString() });
});

// Admin health check
app.get('/admin/health', authenticateAdmin, (req, res) => {
    res.json({ 
        status: 'OK', 
        timestamp: new Date().toISOString(),
        examCount: examStore.size,
        sessionCount: sessionStore.size
    });
});

app.listen(PORT, () => {
    console.log(`🚀 AI Exam IDE Server running on port ${PORT}`);
    console.log(`📁 Workspace directory: ${WORKSPACE_DIR}`);
    console.log(`📤 Submissions directory: ${SUBMISSIONS_DIR}`);
    console.log(`🔄 Caching DISABLED - Fresh exams every time`);
    console.log(`🔐 Admin secret key: ${ADMIN_SECRET_KEY.substring(0, 8)}...`);
    console.log(`🔑 Admin password: ${process.env.ADMIN_PASSWORD || 'admin123!@#'}`);
    
    const javaCheck = runCommand('java', ['-version']);
    const javacCheck = runCommand('javac', ['-version']);
    
    if (javaCheck.code === 0 && javacCheck.code === 0) {
        console.log('✅ Java is available for code execution');
    } else {
        console.log('❌ Java not found - please install Java JDK 17+');
    }
    
    if (OPENAI_API_KEY && OPENAI_API_KEY !== 'your-openai-api-key-here') {
        console.log('✅ OpenAI API key configured');
    } else {
        console.log('⚠️  OpenAI API key not configured - using fallback exams');
    }
    
    console.log('\n📋 Admin API Endpoints:');
    console.log('  POST /admin/login - Get session token');
    console.log('  GET /admin/exams - List all exams');
    console.log('  GET /admin/exam/:examId - Get specific exam');
    console.log('  GET /admin/exam/:examId/tasks - Get exam tasks only');
    console.log('  GET /admin/exam/:examId?format=download - Download exam JSON');
});
```



using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.ViewModels;
using System.Diagnostics;

namespace FediverseHub.Linux;

public sealed class MainWindow : Window
{
    private const double LoadMoreDistance = 700;
    private static readonly HttpClient MediaHttpClient = new();
    private readonly TimelineViewModel _timelineViewModel;
    private readonly ComposeViewModel _composeViewModel;
    private readonly LoginViewModel _loginViewModel;
    private readonly RegistrationViewModel _registrationViewModel;
    private readonly OnboardingViewModel _onboardingViewModel;
    private readonly ILocalizationService _localizationService;
    private readonly TextBlock _composeStatus = new();
    private readonly ComboBox _sourcePicker = new();
    private readonly TextBox _titleBox = new();
    private readonly TextBox _bodyBox = new();
    private readonly TextBox _communityBox = new();
    private readonly TextBox _registrationNameBox = new();
    private readonly TextBox _registrationEmailBox = new();
    private readonly TextBox _registrationPasswordBox = new();
    private readonly TextBox _registrationConfirmPasswordBox = new();
    private readonly TextBlock _registrationPeerName = new();
    private readonly TextBlock _registrationMessage = new();
    private readonly StackPanel _registrationResults = new();
    private readonly Button _registrationContinue = new();
    private readonly TextBox _customHashtagsBox = new();
    private readonly TextBlock _onboardingMessage = new();
    private readonly FediverseSourceType[] _composeSources =
    [
        FediverseSourceType.Mastodon,
        FediverseSourceType.Pixelfed,
        FediverseSourceType.PeerTube,
        FediverseSourceType.Lemmy
    ];

    public MainWindow(
        TimelineViewModel timelineViewModel,
        ComposeViewModel composeViewModel,
        LoginViewModel loginViewModel,
        RegistrationViewModel registrationViewModel,
        OnboardingViewModel onboardingViewModel,
        ILocalizationService localizationService)
    {
        _timelineViewModel = timelineViewModel;
        _composeViewModel = composeViewModel;
        _loginViewModel = loginViewModel;
        _registrationViewModel = registrationViewModel;
        _onboardingViewModel = onboardingViewModel;
        _localizationService = localizationService;

        Title = localizationService.GetString("app.title");
        Width = 1220;
        Height = 820;
        MinWidth = 980;
        MinHeight = 640;
        Content = BuildLoginLayout();
        KeyDown += OnKeyDown;
    }

    private Control BuildLoginLayout()
    {
        var stack = new StackPanel
        {
            Width = 460,
            Spacing = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Children.Add(new TextBlock
        {
            Text = _loginViewModel.Title,
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center
        });
        stack.Children.Add(new TextBlock
        {
            Text = _loginViewModel.Subtitle,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        var login = new Button { Content = _loginViewModel.LoginLabel, HorizontalAlignment = HorizontalAlignment.Stretch };
        login.Click += async (_, _) => await StartAppAsync(readOnly: false);
        stack.Children.Add(login);

        var register = new Button { Content = _loginViewModel.RegisterLabel, HorizontalAlignment = HorizontalAlignment.Stretch };
        register.Click += (_, _) => Content = BuildRegistrationLayout();
        stack.Children.Add(register);

        var skip = new Button { Content = _loginViewModel.DemoModeLabel, HorizontalAlignment = HorizontalAlignment.Stretch };
        skip.Click += async (_, _) => await StartAppAsync(readOnly: true);
        stack.Children.Add(skip);

        stack.Children.Add(new TextBlock
        {
            Text = _loginViewModel.SkipNotice,
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        return stack;
    }

    private Control BuildRegistrationLayout()
    {
        _registrationNameBox.PlaceholderText = _registrationViewModel.AccountNameLabel;
        _registrationNameBox.TextChanged += (_, _) =>
        {
            _registrationViewModel.AccountName = _registrationNameBox.Text ?? string.Empty;
            _registrationPeerName.Text = _registrationViewModel.PeerAccountName;
        };
        _registrationEmailBox.PlaceholderText = _registrationViewModel.EmailLabel;
        _registrationPasswordBox.PlaceholderText = _registrationViewModel.PasswordLabel;
        _registrationPasswordBox.PasswordChar = '*';
        _registrationConfirmPasswordBox.PlaceholderText = _registrationViewModel.ConfirmPasswordLabel;
        _registrationConfirmPasswordBox.PasswordChar = '*';

        var register = new Button
        {
            Content = _registrationViewModel.RegisterLabel,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        register.Click += async (_, _) => await RegisterAccountsAsync();

        _registrationContinue.Content = _registrationViewModel.ContinueLabel;
        _registrationContinue.HorizontalAlignment = HorizontalAlignment.Stretch;
        _registrationContinue.IsVisible = false;
        _registrationContinue.Click += async (_, _) =>
        {
            _registrationViewModel.ContinueAsAuthenticated();
            Content = BuildOnboardingLayout();
        };

        var back = new Button
        {
            Content = _registrationViewModel.BackLabel,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        back.Click += (_, _) => Content = BuildLoginLayout();

        var stack = new StackPanel
        {
            Width = 560,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Children.Add(new TextBlock
        {
            Text = _registrationViewModel.Title,
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center
        });
        stack.Children.Add(new TextBlock
        {
            Text = _registrationViewModel.Explanation,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(_registrationNameBox);
        stack.Children.Add(_registrationEmailBox);
        stack.Children.Add(_registrationPasswordBox);
        stack.Children.Add(_registrationConfirmPasswordBox);
        stack.Children.Add(new TextBlock { Text = _registrationViewModel.PeerAccountNameLabel, FontWeight = FontWeight.Bold });
        stack.Children.Add(_registrationPeerName);
        stack.Children.Add(register);
        stack.Children.Add(_registrationMessage);
        stack.Children.Add(_registrationResults);
        stack.Children.Add(_registrationContinue);
        stack.Children.Add(back);
        return stack;
    }

    private Control BuildOnboardingLayout()
    {
        var interestPanel = new StackPanel { Spacing = 8 };
        foreach (var interest in _onboardingViewModel.Interests)
        {
            var checkBox = new CheckBox
            {
                Content = $"{interest.DisplayName}  {interest.HashtagPreview}",
                IsChecked = interest.IsSelected
            };
            checkBox.IsCheckedChanged += (_, _) => interest.IsSelected = checkBox.IsChecked == true;
            interestPanel.Children.Add(checkBox);
        }

        _customHashtagsBox.PlaceholderText = _onboardingViewModel.CustomHashtagsLabel;

        var complete = new Button
        {
            Content = _onboardingViewModel.CompleteLabel,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        complete.Click += async (_, _) =>
        {
            _onboardingViewModel.CustomHashtags = _customHashtagsBox.Text ?? string.Empty;
            await _onboardingViewModel.CompleteAsync(CancellationToken.None);
            _onboardingMessage.Text = _onboardingViewModel.ValidationMessage;
            if (_onboardingViewModel.ValidationMessage is null)
            {
                Content = BuildLayout();
            }
        };

        var stack = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12
        };
        stack.Children.Add(new TextBlock
        {
            Text = _onboardingViewModel.Title,
            FontSize = 28,
            FontWeight = FontWeight.Bold
        });
        stack.Children.Add(new TextBlock
        {
            Text = _onboardingViewModel.Subtitle,
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new ScrollViewer
        {
            MaxHeight = 460,
            Content = interestPanel
        });
        stack.Children.Add(_customHashtagsBox);
        stack.Children.Add(_onboardingMessage);
        stack.Children.Add(complete);

        return stack;
    }

    private Control BuildLayout()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("220,*,320"),
            RowDefinitions = new RowDefinitions("*")
        };

        var navigation = BuildNavigation();
        Grid.SetColumn(navigation, 0);
        grid.Children.Add(navigation);

        var timeline = BuildTimeline();
        Grid.SetColumn(timeline, 1);
        grid.Children.Add(timeline);

        var compose = BuildComposePanel();
        Grid.SetColumn(compose, 2);
        grid.Children.Add(compose);

        return grid;
    }

    private Control BuildNavigation()
    {
        var stack = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 8
        };

        stack.Children.Add(new TextBlock
        {
            Text = _localizationService.GetString("app.title"),
            FontSize = 22,
            FontWeight = FontWeight.Bold
        });

        stack.Children.Add(CreateNavButton(_timelineViewModel.AllTabTitle, null));
        stack.Children.Add(CreateNavButton(_timelineViewModel.MastodonTabTitle, FediverseSourceType.Mastodon));
        stack.Children.Add(CreateNavButton(_timelineViewModel.PixelfedTabTitle, FediverseSourceType.Pixelfed));
        stack.Children.Add(CreateNavButton(_timelineViewModel.PeerTubeTabTitle, FediverseSourceType.PeerTube));
        stack.Children.Add(CreateNavButton(_timelineViewModel.LemmyTabTitle, FediverseSourceType.Lemmy));
        stack.Children.Add(CreateNavButton(_timelineViewModel.RssTabTitle, FediverseSourceType.Rss));

        var refresh = new Button
        {
            Content = _timelineViewModel.RefreshLabel,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 12, 0, 0)
        };
        refresh.Click += async (_, _) => await RefreshTimelineAsync(_timelineViewModel.SelectedSource);
        stack.Children.Add(refresh);

        var logout = new Button
        {
            Content = _localizationService.GetString("action.logout"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 12, 0, 0)
        };
        logout.Click += (_, _) =>
        {
            _loginViewModel.SignOut();
            Content = BuildLoginLayout();
        };
        stack.Children.Add(logout);

        return new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = stack
        };
    }

    private Button CreateNavButton(string title, FediverseSourceType? sourceType)
    {
        var button = new Button
        {
            Content = title,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        button.Click += async (_, _) => await RefreshTimelineAsync(sourceType);
        return button;
    }

    private Control BuildTimeline()
    {
        var items = new ItemsControl
        {
            Margin = new Thickness(16),
            ItemsSource = _timelineViewModel.Items,
            ItemTemplate = new FuncDataTemplate<TimelineItemViewModel>((item, _) => CreateTimelineCard(item), true)
        };

        var scrollViewer = new ScrollViewer
        {
            Content = items
        };
        scrollViewer.ScrollChanged += async (_, _) =>
        {
            var remaining = scrollViewer.Extent.Height - scrollViewer.Viewport.Height - scrollViewer.Offset.Y;
            if (remaining <= LoadMoreDistance)
            {
                await _timelineViewModel.LoadMoreAsync(CancellationToken.None);
            }
        };

        return scrollViewer;
    }

    private static Control CreateTimelineCard(TimelineItemViewModel? item)
    {
        if (item is null)
        {
            return new TextBlock();
        }

        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(new TextBlock
        {
            Text = $"{item.Source}  {item.PublishedAt}",
            Foreground = Brushes.DodgerBlue,
            FontWeight = FontWeight.Bold
        });
        stack.Children.Add(new TextBlock
        {
            Text = item.Author,
            FontWeight = FontWeight.Bold
        });
        if (!string.IsNullOrWhiteSpace(item.Title))
        {
            stack.Children.Add(new TextBlock
            {
                Text = item.Title,
                IsVisible = item.HasTitle,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap
            });
        }

        stack.Children.Add(new TextBlock
        {
            Text = item.Text,
            IsVisible = item.HasText,
            TextWrapping = TextWrapping.Wrap
        });

        foreach (var imageUrl in item.ImageUrls)
        {
            stack.Children.Add(CreateRemoteImage(imageUrl));
        }

        if (item.HasVideo)
        {
            if (!string.IsNullOrWhiteSpace(item.MediaPreviewUrl))
            {
                stack.Children.Add(CreateRemoteImage(item.MediaPreviewUrl));
            }

            var video = new Button
            {
                Content = "Video oeffnen",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            video.Click += (_, _) => OpenUrl(item.VideoEmbedUrl ?? item.OpenUrl);
            stack.Children.Add(video);
        }

        var border = new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(8),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Child = stack
        };
        border.PointerPressed += (_, args) =>
        {
            if (args.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
            {
                OpenUrl(item.OpenUrl);
            }
        };

        return border;
    }

    private static Control CreateRemoteImage(string imageUrl)
    {
        var image = new Image
        {
            Height = 260,
            Stretch = Stretch.UniformToFill
        };
        _ = LoadRemoteImageAsync(image, imageUrl);
        return image;
    }

    private static async Task LoadRemoteImageAsync(Image image, string imageUrl)
    {
        try
        {
            await using var stream = await MediaHttpClient.GetStreamAsync(imageUrl).ConfigureAwait(false);
            var bitmap = new Bitmap(stream);
            await Dispatcher.UIThread.InvokeAsync(() => image.Source = bitmap);
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() => image.IsVisible = false);
        }
    }

    private static void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Opening the original is best-effort on desktop platforms.
        }
    }

    private Control BuildComposePanel()
    {
        _sourcePicker.ItemsSource = _composeSources;
        _sourcePicker.SelectedIndex = 0;
        _sourcePicker.SelectionChanged += (_, _) =>
        {
            if (_sourcePicker.SelectedIndex >= 0)
            {
                _composeViewModel.SelectedSource = _composeSources[_sourcePicker.SelectedIndex];
            }
        };

        _titleBox.PlaceholderText = _composeViewModel.TitleLabel;
        _bodyBox.PlaceholderText = _composeViewModel.BodyLabel;
        _bodyBox.AcceptsReturn = true;
        _bodyBox.MinHeight = 160;
        _communityBox.PlaceholderText = _composeViewModel.CommunityLabel;

        var publish = new Button
        {
            Content = _composeViewModel.PublishLabel,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        publish.Click += async (_, _) => await PublishAsync();

        var stack = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10
        };
        stack.Children.Add(new TextBlock
        {
            Text = _localizationService.GetString("nav.compose"),
            FontSize = 22,
            FontWeight = FontWeight.Bold
        });
        stack.Children.Add(new TextBlock { Text = _composeViewModel.SourceLabel });
        stack.Children.Add(_sourcePicker);
        stack.Children.Add(_titleBox);
        stack.Children.Add(_bodyBox);
        stack.Children.Add(_communityBox);
        stack.Children.Add(publish);
        stack.Children.Add(_composeStatus);

        return new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = stack
        };
    }

    private async Task RefreshTimelineAsync(FediverseSourceType? sourceType)
    {
        _timelineViewModel.SelectedSource = sourceType;
        await _timelineViewModel.RefreshAsync(CancellationToken.None);
    }

    private Task StartAppAsync(bool readOnly)
    {
        if (readOnly)
        {
            _loginViewModel.StartReadOnlySession();
        }
        else
        {
            _loginViewModel.StartAuthenticatedSession();
        }

        Content = BuildLayout();
        return Task.CompletedTask;
    }

    private async Task RegisterAccountsAsync()
    {
        _registrationViewModel.AccountName = _registrationNameBox.Text ?? string.Empty;
        _registrationViewModel.Email = _registrationEmailBox.Text ?? string.Empty;
        _registrationViewModel.Password = _registrationPasswordBox.Text ?? string.Empty;
        _registrationViewModel.ConfirmPassword = _registrationConfirmPasswordBox.Text ?? string.Empty;
        await _registrationViewModel.RegisterAsync(CancellationToken.None);
        _registrationMessage.Text = _registrationViewModel.Message;
        _registrationResults.Children.Clear();

        foreach (var result in _registrationViewModel.Results)
        {
            _registrationResults.Children.Add(new TextBlock
            {
                Text = $"{result.Provider}: {result.AccountHandle} ({result.State})",
                TextWrapping = TextWrapping.Wrap
            });
        }

        _registrationContinue.IsVisible = _registrationViewModel.CanContinue;
    }

    private async Task PublishAsync()
    {
        _composeViewModel.Title = _titleBox.Text ?? string.Empty;
        _composeViewModel.Text = _bodyBox.Text ?? string.Empty;
        _composeViewModel.CommunityName = _communityBox.Text ?? string.Empty;
        await _composeViewModel.PublishAsync(CancellationToken.None);
        _composeStatus.Text = _composeViewModel.StatusMessage;
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.R:
                await RefreshTimelineAsync(_timelineViewModel.SelectedSource);
                break;
            case Key.N:
                _bodyBox.Focus();
                break;
            case Key.D1:
                await RefreshTimelineAsync(null);
                break;
            case Key.D2:
                await RefreshTimelineAsync(FediverseSourceType.Mastodon);
                break;
            case Key.D3:
                await RefreshTimelineAsync(FediverseSourceType.Pixelfed);
                break;
            case Key.D4:
                await RefreshTimelineAsync(FediverseSourceType.PeerTube);
                break;
            case Key.D5:
                await RefreshTimelineAsync(FediverseSourceType.Lemmy);
                break;
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Timers;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls : Control
	{
		private Windows.Media.Playback.MediaPlayer _mediaPlayer;
		private SerialDisposable _mediaPlayerSubscriptions = new();

		// The player will be temporarily paused while the progress slider is being manipulated.
		// This flag prevents the update of play/pause button while that happens.
		private bool _skipPlayPauseStateUpdate;
		private bool _isScrubbing;

		internal void SetMediaPlayer(Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			_mediaPlayerSubscriptions.Disposable = null;

			_mediaPlayer = mediaPlayer;

			BindMediaPlayer();
		}

		internal void SetMediaPlayerElement(MediaPlayerElement mediaPlayerElement)
		{
			_mpe = mediaPlayerElement;
		}

		private void BindMediaPlayer()
		{
			if (_mediaPlayer is null)
			{
				return;
			}

			_mediaPlayerSubscriptions.Disposable = null;

			_mediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
			_mediaPlayer.PlaybackSession.BufferingProgressChanged += OnBufferingProgressChanged;
			_mediaPlayer.PlaybackSession.NaturalDurationChanged += OnNaturalDurationChanged;
			_mediaPlayer.PlaybackSession.PositionChanged += OnPositionChanged;

			_mediaPlayerSubscriptions.Disposable = Disposable.Create(() =>
			{
				if (_mediaPlayer is { })
				{
					_mediaPlayer.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
					_mediaPlayer.PlaybackSession.BufferingProgressChanged -= OnBufferingProgressChanged;
					_mediaPlayer.PlaybackSession.NaturalDurationChanged -= OnNaturalDurationChanged;
					_mediaPlayer.PlaybackSession.PositionChanged -= OnPositionChanged;
				}
			});
		}

		public void TappedProgressSlider(object sender, RoutedEventArgs e)
		{
			if (double.IsNaN(m_tpMediaPositionSlider.Value))
			{
				return;
			}
			if (_mediaPlayer != null)
			{
				_wasPlaying = _mediaPlayer.PlaybackSession.IsPlaying;
				_skipPlayPauseStateUpdate = true;

				_mediaPlayer.Pause();
				_mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(m_tpMediaPositionSlider.Value);
				if (_wasPlaying)
				{
					_mediaPlayer.Play();
				}

				_skipPlayPauseStateUpdate = false;
			}
		}

		private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
		{
			var state = (MediaPlaybackState)args;

			switch (state)
			{
				case MediaPlaybackState.Opening:
				case MediaPlaybackState.Paused:
				case MediaPlaybackState.None:
					_mediaPlayer.PlaybackSession.UpdateTimePositionRate = 0;
					CancelControlsVisibilityTimer();
					break;
				case MediaPlaybackState.Playing:
					_mediaPlayer.PlaybackSession.UpdateTimePositionRate = 0;
					ResetControlsVisibilityTimer();
					break;
				case MediaPlaybackState.Buffering:
					break;
			}

			// skip transition, as the event may originate from non-ui thread
			UpdateMediaStates(useTransition: false);
			if (!_skipPlayPauseStateUpdate)
			{
				UpdatePlayPauseStates(useTransition: false);
			}
		}

		private void OnBufferingProgressChanged(MediaPlaybackSession sender, object args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				m_tpDownloadProgressIndicator.Maybe(p => p.Value = (double)args);
			});
		}

		private void OnNaturalDurationChanged(MediaPlaybackSession sender, object args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var duration = args as TimeSpan? ?? TimeSpan.Zero;
				m_tpMediaPositionSlider.Maybe(p => p.Minimum = 0);
				m_tpMediaPositionSlider.Maybe(p => p.Maximum = duration.TotalSeconds);

				if (_mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing
					&& _mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Paused)
				{
					m_tpTimeRemainingElement.Maybe<TextBlock>(p => p.Text = FormatTime(duration));
				}
			});
		}

		private void OnPositionChanged(MediaPlaybackSession sender, object args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var elapsed = args as TimeSpan? ?? TimeSpan.Zero;
				m_tpTimeElapsedElement.Maybe<TextBlock>(p => p.Text = FormatTime(elapsed));
				m_tpMediaPositionSlider.Maybe(p => p.Value = elapsed.TotalSeconds);

				var remaining = _mediaPlayer.PlaybackSession.NaturalDuration - elapsed;
				m_tpTimeRemainingElement.Maybe<TextBlock>(p => p.Text = FormatTime(remaining));
				_ = UpdateTimePosition(elapsed);
			});
		}

		public async Task UpdateTimePosition(TimeSpan elapsed)
		{
			if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
				&& !_mediaPlayer.PlaybackSession.IsUpdateTimePosition
				&& elapsed != TimeSpan.Zero
				&& _mediaPlayer.PlaybackSession.UpdateTimePositionRate != 0
				&& _mediaPlayer.PlaybackSession.UpdateTimePositionRate != 1)
			{
				_mediaPlayer.PlaybackSession.IsUpdateTimePosition = true;
				elapsed += TimeSpan.FromSeconds(_mediaPlayer.PlaybackSession.UpdateTimePositionRate);
				_mediaPlayer.PlaybackSession.Position = elapsed;
				await Task.Delay(250);
				_mediaPlayer.PlaybackSession.IsUpdateTimePosition = false;
			}
		}

		private void ResetProgressSlider()
		{
			var elapsed = TimeSpan.Zero;
			m_tpTimeElapsedElement.Maybe<TextBlock>(p => p.Text = FormatTime(elapsed));
			m_tpMediaPositionSlider.Maybe(p => p.Value = elapsed.TotalSeconds);

			var remaining = _mediaPlayer.PlaybackSession.NaturalDuration - elapsed;
			m_tpTimeRemainingElement.Maybe<TextBlock>(p => p.Text = FormatTime(remaining));
			_mediaPlayer.PlaybackSession.Position = elapsed;
			_mediaPlayer.PlaybackSession.PositionFromPlayer = elapsed;
		}

		private string FormatTime(TimeSpan time)
		{
			return $"{time.TotalHours:0}:{time.Minutes:00}:{time.Seconds:00}";
		}

		private void PlayPause(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer.PlaybackSession.IsPlaying)
			{
				_mediaPlayer.Pause();
			}
			else
			{
				_mediaPlayer.Play();
			}
		}

		private void Stop(object sender, RoutedEventArgs e)
		{
			_skipPlayPauseStateUpdate = false;

			_mediaPlayer.Pause();
			ResetProgressSlider();
			_mediaPlayer.Stop();
		}

		private void SkipBackward(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.Position -= TimeSpan.FromSeconds(10);
		}

		private void SkipForward(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.Position += TimeSpan.FromSeconds(30);
		}

		private void ForwardButton(object sender, RoutedEventArgs e)
		{
#if __SKIA__ || __WASM__
			_mediaPlayer.PlaybackRate = (_mediaPlayer.PlaybackRate < 0 ? 0 : _mediaPlayer.PlaybackRate) + 0.25;
			_mediaPlayer.PlaybackSession.UpdateTimePositionRate = 0;
#else
			_mediaPlayer.PlaybackSession.UpdateTimePositionRate =
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate < 1 ? 1 : /*To stop the Rewind*/
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate * 2; /*To start the Forward*/
#endif
		}

		private void RewindButton(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.UpdateTimePositionRate =
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate > 1 ? 1 : /*To stop the Forward*/
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate == 1 ? -1 : /*To start the Rewind*/
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate == 0 ? -1 : /*To start the Rewind*/
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate * 2;
		}

		private void OnVolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			_mediaPlayer.Volume = e.NewValue;
			UpdateVolumeMuteStates();
			ResetControlsVisibilityTimer();
		}

		private void ToggleMute(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.IsMuted = !_mediaPlayer.IsMuted;
			UpdateVolumeMuteStates(isExplicitMuteToggle: true);
			ResetControlsVisibilityTimer();
		}

		private void ThumbOnDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
		{
			if (double.IsNaN(m_tpMediaPositionSlider.Value))
			{
				return;
			}

			if (_mediaPlayer != null)
			{
				_mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(m_tpMediaPositionSlider.Value);

				if (_wasPlaying)
				{
					_mediaPlayer.Play();
				}
			}

			_isScrubbing = false;
			_skipPlayPauseStateUpdate = false;
		}

		private void ThumbOnDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
		{
			if (_mediaPlayer != null && !_isScrubbing)
			{
				_wasPlaying = _mediaPlayer.PlaybackSession.IsPlaying;
				_skipPlayPauseStateUpdate = true;
				_isScrubbing = true;

				_mediaPlayer.Pause();
			}
		}
	}
}

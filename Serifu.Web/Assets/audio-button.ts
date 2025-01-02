import { assertDefined } from './util';

type PlayerState = 'stopped' | 'loading' | 'playing';

class AudioButton extends HTMLElement {
  public static current: AudioButton | undefined;

  private icon: HTMLElement = null!;
  private audio: HTMLAudioElement = null!;

  private _state: PlayerState = 'stopped';

  connectedCallback() {
    this.icon = assertDefined(this.querySelector('i'), 'icon');
    this.audio = assertDefined(this.querySelector('audio'), 'audio');

    this.addEventListener('click', this.onClick);
    this.addEventListener('keydown', this.onKeyDown);
    this.audio.addEventListener('waiting', this.onLoading);
    this.audio.addEventListener('playing', this.onPlaying);
    this.audio.addEventListener('pause', this.onStopped);
  }

  disconnectedCallback() {
    this.stop();
    this.removeEventListener('click', this.onClick);
    this.removeEventListener('keydown', this.onKeyDown);
    this.audio.removeEventListener('waiting', this.onLoading);
    this.audio.removeEventListener('playing', this.onPlaying);
    this.audio.removeEventListener('pause', this.onStopped);

    if (AudioButton.current === this) {
      AudioButton.current = undefined;
    }
  }

  onClick = () => {
    if (this.state === 'stopped') {
      this.play();
    } else {
      this.stop();
    }
  };

  onKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      this.onClick();
    }
  };

  onLoading = () => {
    console.log('Loading:', this.audio.src);
    this.state = 'loading';
  };

  onPlaying = () => {
    console.log('Playing:', this.audio.src);
    this.state = 'playing';
  };

  onStopped = () => {
    console.log('Stopped:', this.audio.src);
    this.state = 'stopped';
  };

  play() {
    if (this.state !== 'stopped') {
      return;
    }
    console.log('▶ Play:', this.audio.src);
    AudioButton.current?.stop();
    AudioButton.current = this;
    this.audio.play();
  }

  stop() {
    if (this.state === 'stopped') {
      return;
    }
    console.log('⏹ Stop:', this.audio.src);
    this.audio.pause();
    this.audio.currentTime = 0;
  }

  get state() {
    return this._state;
  }

  set state(newState: PlayerState) {
    this._state = newState;
    switch (newState) {
      case 'stopped':
        this.icon.className = 'icon icon-play';
        break;
      case 'loading':
        this.icon.className = 'icon icon-loading';
        break;
      case 'playing':
        this.icon.className = 'icon icon-stop';
        break;
    }
  }
}

customElements.define('audio-button', AudioButton);

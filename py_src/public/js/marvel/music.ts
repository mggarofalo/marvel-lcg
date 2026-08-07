import { ButtonSetting, Setting } from "./settings.js";

export class Music {
    private static sound_bgm = document.getElementById('play-bgm') as HTMLAudioElement;
    private static sound_cache: Record<string, HTMLAudioElement> = {};

    static playBgm() {
        console.log('Play BGM');
        Music.sound_bgm.src = "./asset/marvel_s_black_widow_official_theme_epic_version.mp3";
        Music.sound_bgm.volume = 0.05;
        Music.sound_bgm.play();
    }

    static pauseBgm() {
        Music.sound_bgm.pause();
    }

    static playClickCardSound() {
        Music.doPlaySound('flip.wav');
    }

    static preloadSound(sound_name: string) {
        if (!Music.sound_cache[sound_name]) {
            const audio = new Audio(sound_name);
            Music.sound_cache[sound_name] = audio;
        }
    }

    static doPlaySound(sound_name: string) {

        if( !Setting.is_debug && document.visibilityState != 'visible'){
            return;
        }

        if (!ButtonSetting.music_on) {
            return;
        }

        console.log("Sound:", sound_name)

        // Preload the sound if not already in cache
        Music.preloadSound(sound_name);

        // Use the cached sound
        const sound = Music.sound_cache[sound_name];
        sound.volume = 0.2;

        // Stop the current sound if it's playing
        if (!sound.paused) {
            sound.pause();
            sound.currentTime = 0; // Reset to the beginning
        }

        sound.play().catch((error) => {
        });
    }
}

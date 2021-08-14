/**
 * @author Alexander Manzyuk <admsev@gmail.com>
 * Copyright (c) 2012 Alexander Manzyuk - released under MIT License
 * https://github.com/admsev/jquery-play-sound
 * Usage: $.playSound('http://example.org/sound')
 * $.playSound('http://example.org/sound.wav')
 * $.playSound('/attachments/sounds/1234.wav')
 * $.playSound('/attachments/sounds/1234.mp3')
 * $.stopSound();
**/

(function ($) {
    $.extend({
        playSound: function () {
            var isChrome = /Chrome/.test(navigator.userAgent) && /Google Inc/.test(navigator.vendor);
            if (!isChrome) {
                return $(
                    ''
                    + ''
                    + ''
                    + ''
                ).appendTo('body');
                //console.log("Not Chrome");
            }
            else {
                return $('<iframe src="' + arguments[0] + '" allow="autoplay" style="display:none" id="iframeAudio"></iframe>').appendTo('body');
                //console.log("Chrome"); //just to make sure that it will not have 2x audio in the background
            }
        },
        stopSound: function () {
            $(".sound-player").remove();
        }
    });
})(jQuery);
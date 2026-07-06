(function () {
    'use strict';

    var tiers = [
        { id: 'donate-btn-fuel', hosted_button_id: '6ZS7K96DSKKF8' },
        { id: 'donate-btn-boost', hosted_button_id: 'M5D5XMPXK2W4L' },
        { id: 'donate-btn-fleet', hosted_button_id: 'M5D5XMPXK2W4L' }
    ];

    function renderButtons() {
        if (!window.PayPal || !window.PayPal.Donation) return;
        tiers.forEach(function (tier) {
            if (!document.getElementById(tier.id)) return;
            PayPal.Donation.Button({
                env: 'production',
                hosted_button_id: tier.hosted_button_id,
                image: {
                    src: 'https://www.paypalobjects.com/en_US/GB/i/btn/btn_donateCC_LG.gif',
                    alt: 'Donate with PayPal button',
                    title: 'PayPal - The safer, easier way to pay online!'
                }
            }).render('#' + tier.id);
        });
    }

    function init() {
        if (document.querySelector('.donation-board') === null) return;
        var script = document.createElement('script');
        script.src = 'https://www.paypalobjects.com/donate/sdk/donate-sdk.js';
        script.charset = 'UTF-8';
        script.onload = renderButtons;
        script.onerror = function () {
            document.querySelectorAll('.donate-paypal-slot').forEach(function (slot) {
                slot.innerHTML = '<p class="donate-fallback">PayPal unavailable — scan the QR code or visit <a href="https://truckdeck.site/downloads" target="_blank" rel="noopener">truckdeck.site</a>.</p>';
            });
        };
        document.head.appendChild(script);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

/* global window, document, Intl */
// One client-side source for formatting timestamp values returned by Sentinel.
// The values are emitted by Razor into data attributes and are HTML encoded.
(function () {
    "use strict";

    const root = document.documentElement;
    const locale = root.dataset.sentinelLocale || "en-AU";
    const timeZone = root.dataset.sentinelTimeZone || "Etc/UTC";

    function format(value, options) {
        if (!value) {
            return "";
        }

        const date = value instanceof Date ? value : new Date(value);
        if (Number.isNaN(date.getTime())) {
            return "";
        }

        try {
            return new Intl.DateTimeFormat(locale, {
                timeZone,
                ...options
            }).format(date);
        } catch (_) {
            // A legacy non-IANA zone can exist in an older configuration. The
            // server still formats it correctly; avoid a client-side failure
            // while settings normalise it on the next valid save.
            return new Intl.DateTimeFormat(locale, options).format(date);
        }
    }

    // A date-only value (such as date of birth or notification date) is a
    // calendar day, not an instant. Parsing YYYY-MM-DD as a Date treats it as
    // UTC and can display the preceding day in some browser time zones.
    function formatDateOnly(value) {
        if (!value) {
            return "";
        }

        const match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})/);
        if (!match) {
            return format(value, { year: "numeric", month: "short", day: "numeric" });
        }

        const date = new Date(Date.UTC(
            Number.parseInt(match[1], 10),
            Number.parseInt(match[2], 10) - 1,
            Number.parseInt(match[3], 10),
            12));

        return new Intl.DateTimeFormat(locale, {
            timeZone: "UTC",
            year: "numeric",
            month: "short",
            day: "numeric"
        }).format(date);
    }

    window.SentinelRegionalFormatting = Object.freeze({
        locale,
        timeZone,
        formatDateOnly,
        formatDate: (value) => format(value, { year: "numeric", month: "short", day: "numeric" }),
        formatDateTime: (value) => format(value, {
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        }),
        formatDateTimeLong: (value) => format(value, {
            dateStyle: "full",
            timeStyle: "long"
        }),
        formatTime: (value) => format(value, { hour: "2-digit", minute: "2-digit" })
    });
}());

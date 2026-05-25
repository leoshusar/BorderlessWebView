import { isYoutube, handleYoutube } from "./youtube.js";

const handlers = [
    { check: isYoutube, handle: handleYoutube }
];

chrome.action.onClicked.addListener(async (tab) => {
    if (!tab.id || !tab.url) {
        return;
    }

    const url = new URL(tab.url);

    let finalUrl = tab.url;

    for (const handler of handlers) {
        if (handler.check(url)) {
            finalUrl = await handler.handle(tab) ?? finalUrl;
            break;
        }
    }

    chrome.runtime.sendNativeMessage(
        "cz.leoshusar.borderlesswebview",
        { url: finalUrl }
    );
});

export const isYoutube = (url) =>
    url.hostname.includes("youtube.com") && (url.pathname.startsWith("/watch") || url.pathname.startsWith("/shorts/"));

export const handleYoutube = async (tab) => {
    try {
        const [result] = await chrome.scripting.executeScript({
            target: { tabId: tab.id },
            func: () => {
                const player = document.querySelector("video");
                if (!player) {
                    return null;
                }

                if (!player.paused) {   
                    player.pause();
                }

                return Math.floor(player.currentTime);
            }
        });

        if (!result || typeof result.result !== "number") {
            return null;
        }

        const seconds = Math.max(result.result - 5, 0);

        const url = new URL(tab.url);
        url.searchParams.set("t", `${seconds}s`);

        return url.toString();
    } catch (error) {
        console.error("Error executing script:", error);
        return null;
    }
}
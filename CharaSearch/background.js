chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "searchCharacterImage",
    title: "この画像のキャラを検索",
    contexts: ["image"]
  });
});

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId === "searchCharacterImage") {
    const imageUrl = info.srcUrl;

    chrome.tabs.sendMessage(tab.id, { action: "showLoading" });

    try {
      const base64Data = await getBase64FromUrl(imageUrl);
      console.log("Base64変換成功:", base64Data.substring(0, 50) + "...");

      const result = await fetchCharacterData(base64Data);

      chrome.tabs.sendMessage(tab.id, {
        action: "showResult",
        data: result
      });
    } catch (error) {
      console.error("画像取得エラー:", error);
      chrome.tabs.sendMessage(tab.id, { action: "showError" });
    }
  }
});

async function getBase64FromUrl(url) {
  const response = await fetch(url);
  const blob = await response.blob();
  
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result);
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
}

async function fetchCharacterData(base64Image) {
  const serverUrl = "http://localhost:5000/api/search"; 

  const response = await fetch(serverUrl, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      imageBase64: base64Image
    })
  });

  if (!response.ok) {
    throw new Error("サーバーエラー");
  }

  const data = await response.json();
  return {
    character: data.character,
    title: data.title
  };
}
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === "showLoading") {
    showToast("画像を解析中...");
  } else if (request.action === "showResult") {
    showToast(`キャラ: ${request.data.character}<br>作品: ${request.data.title}`);
  } else if (request.action === "showError") {
    showToast("解析に失敗しました");
  }
});

function showToast(htmlContent) {
  const existingToast = document.getElementById("chara-search-toast");
  if (existingToast) existingToast.remove();

  const toast = document.createElement("div");
  toast.id = "chara-search-toast";
  toast.innerHTML = htmlContent;

  Object.assign(toast.style, {
    position: "fixed",
    bottom: "20px",
    right: "20px",
    padding: "15px 20px",
    backgroundColor: "rgba(0, 0, 0, 0.85)",
    color: "#ffffff",
    borderRadius: "8px",
    boxShadow: "0 4px 12px rgba(0,0,0,0.3)",
    zIndex: "2147483647",
    fontFamily: "sans-serif",
    fontSize: "14px",
    lineHeight: "1.6",
    transition: "opacity 0.3s ease-in-out"
  });

  document.body.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = "0";
    setTimeout(() => toast.remove(), 300);
  }, 5000);
}
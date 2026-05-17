import http from "node:http";

const port = Number(process.env.ECHO_ROOM_PROXY_PORT || 8787);
const model = process.env.GEMINI_MODEL || "gemini-2.5-flash";

function readJson(req) {
  return new Promise((resolve, reject) => {
    let body = "";
    req.setEncoding("utf8");
    req.on("data", chunk => {
      body += chunk;
      if (body.length > 1024 * 1024) {
        reject(new Error("Request too large"));
        req.destroy();
      }
    });
    req.on("end", () => {
      try {
        resolve(body ? JSON.parse(body) : {});
      } catch (error) {
        reject(error);
      }
    });
    req.on("error", reject);
  });
}

function send(res, status, data) {
  const text = JSON.stringify(data);
  res.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "access-control-allow-origin": "*",
    "access-control-allow-methods": "POST, OPTIONS",
    "access-control-allow-headers": "content-type"
  });
  res.end(text);
}

function extractJson(text) {
  const cleaned = String(text || "")
    .replace(/^```json\s*/i, "")
    .replace(/^```\s*/i, "")
    .replace(/```$/i, "")
    .trim();

  try {
    return JSON.parse(cleaned);
  } catch {
    const start = cleaned.indexOf("{");
    const end = cleaned.lastIndexOf("}");
    if (start >= 0 && end > start) {
      return JSON.parse(cleaned.slice(start, end + 1));
    }
    throw new Error("Model did not return JSON");
  }
}

function buildPrompt(payload) {
  const language = payload.language || "Chinese";
  const sharedRules = [
    "你是 Echo Room 派对棋盘游戏的主持人。",
    "只返回严格 JSON，不要 Markdown，不要解释。",
    `所有可见文案必须使用 ${language}。`,
    "问题必须具体、能立刻执行，不能抽象空泛。",
    "必须贴合玩家输入的背景主题。",
    "避免投票类玩法。",
    "如果只有 2 名玩家，不要生成需要多人围观、投票、轮流评价的问题。"
  ].join("\n");

  if (payload.kind === "scene") {
    return `${sharedRules}

背景主题：${payload.prompt || "朋友围坐玩真心话大冒险"}

返回这个 JSON：
{
  "scene": {
    "SceneName": "2到6个汉字的场景名",
    "Atmosphere": "一句氛围描述",
    "ChallengeStyle": "一句出题风格",
    "SceneContext": "保留并优化背景主题",
    "OpeningLine": "一句开场白，说明先到目标分获胜",
    "TruthPoints": 1,
    "DarePoints": 2,
    "ScoreLabel": "勇气",
    "MoodTags": ["具体", "现场", "互动"],
    "PreviewChallengeTruth": "一个示例真心话",
    "PreviewChallengeDare": "一个示例大冒险",
    "OriginalPrompt": "原始背景主题"
  }
}`;
  }

  if (payload.kind === "challenge") {
    const isTruth = payload.type === "Truth";
    return `${sharedRules}

背景主题：${payload.sceneContext || "今晚的现场"}
当前玩家：${payload.playerName || "当前玩家"}
玩家人数：${payload.playerCount || 2}
挑战类型：${isTruth ? "真心话" : "大冒险"}

返回这个 JSON：
{
  "challenge": {
    "Challenge": "一句具体题目。要点名当前玩家，能现场完成，符合人数。",
    "Tag": "${isTruth ? "真心话" : "行动"}",
    "Angle": "题目的角度",
    "RewardDesc": "${isTruth ? "+1" : "+2"}",
    "Difficulty": ${isTruth ? 1 : 2}
  }
}`;
  }

  return `${sharedRules}

背景主题：${payload.sceneContext || "今晚的现场"}
当前玩家：${payload.playerName || "当前玩家"}
玩家人数：${payload.playerCount || 2}

返回这个 JSON：
{
  "fortune": {
    "Title": "2到6个汉字的命运卡名",
    "Effect": "一句具体效果描述",
    "EffectType": "bonus",
    "Value": 1,
    "FlavorText": "一句短气氛文案"
  }
}

EffectType 只能是 bonus、penalty、skip、steal 之一。Value 用整数，penalty 的 Value 也写正数。`;
}

async function callGemini(payload) {
  const apiKey = process.env.GEMINI_API_KEY;
  if (!apiKey) {
    const error = new Error("Missing GEMINI_API_KEY");
    error.status = 400;
    throw error;
  }

  const url = `https://generativelanguage.googleapis.com/v1beta/models/${encodeURIComponent(model)}:generateContent?key=${encodeURIComponent(apiKey)}`;
  const prompt = buildPrompt(payload);
  const response = await fetch(url, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({
      contents: [
        {
          role: "user",
          parts: [{ text: prompt }]
        }
      ],
      generationConfig: {
        temperature: 0.9,
        responseMimeType: "application/json"
      }
    })
  });

  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    const message = data?.error?.message || `Gemini API ${response.status}`;
    const error = new Error(message);
    error.status = response.status;
    throw error;
  }

  const text = data?.candidates?.[0]?.content?.parts?.map(part => part.text || "").join("\n") || "";
  return extractJson(text);
}

const server = http.createServer(async (req, res) => {
  if (req.method === "OPTIONS") {
    send(res, 204, {});
    return;
  }

  if (req.method !== "POST" || req.url !== "/echo-room") {
    send(res, 404, { error: "Not found" });
    return;
  }

  try {
    const payload = await readJson(req);
    const result = await callGemini(payload);
    send(res, 200, result);
  } catch (error) {
    send(res, error.status || 500, { error: error.message || "Proxy error" });
  }
});

server.listen(port, "127.0.0.1", () => {
  console.log(`Echo Room Gemini proxy running at http://127.0.0.1:${port}/echo-room`);
  console.log(`Model: ${model}`);
});

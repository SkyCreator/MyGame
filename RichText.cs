using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RichText : Text, IPointerClickHandler
{
    /// <summary>
    /// Resources 文件夹表情目录
    /// </summary>
    [SerializeField]
    private static string ResourcesUrl = "ChatFaceIcon";

    /// <summary>
    /// 图片池
    /// </summary>
    private readonly List<Image> m_ImagesPool = new List<Image>();

    /// <summary>
    /// 图片的最后一个顶点的索引
    /// </summary>
    private readonly List<int> m_ImagesVertexIndex = new List<int>();

    /// <summary>
    /// 正则取出所需要的属性
    /// </summary>
    private static readonly Regex s_Regex =
        new Regex(@"<quad name=(.+?) size=(\d*\.?\d+%?) width=(\d*\.?\d+%?) />", RegexOptions.Singleline);

    public override void SetVerticesDirty()
    {
        base.SetVerticesDirty();
        UpdateQuadImage();
    }

    /// <summary>
    /// 解析完最终的文本
    /// </summary>
    private string m_OutputText;

    protected void UpdateQuadImage()
    {
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.GetPrefabType(this) == UnityEditor.PrefabType.Prefab)
        {
            return;
        }
#endif
        m_OutputText = GetOutputText();
        m_ImagesVertexIndex.Clear();
        foreach (Match match in s_Regex.Matches(m_OutputText))
        {
            var picIndex = match.Index + match.Length - 1;
            var endIndex = picIndex * 4 + 3;
            m_ImagesVertexIndex.Add(endIndex);

            m_ImagesPool.RemoveAll(image => image == null);
            if (m_ImagesPool.Count == 0)
            {
                GetComponentsInChildren<Image>(m_ImagesPool);
            }
            if (m_ImagesVertexIndex.Count > m_ImagesPool.Count)
            {
                var go = new GameObject("Icon");
                Image go_img = go.AddComponent<Image>();
                go.layer = gameObject.layer;
                var rt = go.transform as RectTransform;
                if (rt)
                {
                    rt.SetParent(rectTransform);
                    rt.localPosition = Vector3.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }
                m_ImagesPool.Add(go_img);
            }

            var spriteName = match.Groups[1].Value;
            var size = float.Parse(match.Groups[2].Value);
            var img = m_ImagesPool[m_ImagesVertexIndex.Count - 1];
            if (img.sprite == null || img.sprite.name != spriteName)
            {
                img.sprite = Resources.Load<Sprite>(ResourcesUrl + "/" + spriteName);
            }
            img.rectTransform.sizeDelta = new Vector2(size, size);
            img.enabled = true;
        }

        for (var i = m_ImagesVertexIndex.Count; i < m_ImagesPool.Count; i++)
        {
            if (m_ImagesPool[i] && !m_ImagesPool[i].GetComponent<Canvas>())
            {
                m_ImagesPool[i].enabled = false;
            }
            else
                m_ImagesPool[i].GetComponent<Canvas>().sortingOrder = -1;
                //Canvas.ForceUpdateCanvases();
        }
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        var orignText = m_Text;
        m_Text = GetOutputText();
        base.OnPopulateMesh(toFill);
        m_Text = orignText;

        List<Vector3> verts = new List<Vector3>();
        UIVertex itemUIVertex = new UIVertex();
        for (int i = 0; i < toFill.currentVertCount; i++)
        {
            toFill.PopulateUIVertex(ref itemUIVertex, i);
            verts.Add(itemUIVertex.position);
        }
        //foreach (UIVertex itemUIVertex in vbo)
        //{
        //    verts.Add(itemUIVertex.position);
        //}
        UIVertex vert = new UIVertex();
        for (var i = 0; i < m_ImagesVertexIndex.Count; i++)
        {
            var endIndex = m_ImagesVertexIndex[i];
            var rt = m_ImagesPool[i].rectTransform;
            var size = rt.sizeDelta;
            if (endIndex < verts.Count)
            {
                rt.anchoredPosition = new Vector2(verts[endIndex].x + size.x / 2, verts[endIndex].y + size.y / 2);
                //var pos = verts[endIndex - 3];
                // 抹掉左下角的小黑点
                //for (int j = endIndex, m = endIndex - 3; j > m; j--)
                //{
                //    verts[endIndex] = pos;
                //    verts[j] = verts[m];
                //}
                toFill.PopulateUIVertex(ref vert, endIndex - 3);
                var pos = vert.position;
                for (int j = endIndex, m = endIndex - 3; j > m; j--)
                {
                    toFill.PopulateUIVertex(ref vert, endIndex);
                    vert.position = pos;
                    toFill.SetUIVertex(vert, j);
                }
            }
        }

        if (m_ImagesVertexIndex.Count != 0)
        {
            m_ImagesVertexIndex.Clear();
        }

        // 处理超链接包围框
        foreach (var hrefInfo in m_HrefInfos)
        {
            hrefInfo.boxes.Clear();
            if (hrefInfo.startIndex >= verts.Count)
            {
                continue;
            }

            // 将超链接里面的文本顶点索引坐标加入到包围框
            var pos = verts[hrefInfo.startIndex];
            var bounds = new Bounds(pos, Vector3.zero);
            for (int i = hrefInfo.startIndex, m = hrefInfo.endIndex; i < m; i++)
            {
                if (i >= verts.Count)
                {
                    break;
                }

                pos = verts[i];
                if (pos.x < bounds.min.x) // 换行重新添加包围框
                {
                    hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
                    bounds = new Bounds(pos, Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(pos); // 扩展包围框
                }
            }
            hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
        }
    }

    /// <summary>
    /// 超链接信息列表
    /// </summary>
    private readonly List<HrefInfo> m_HrefInfos = new List<HrefInfo>();

    /// <summary>
    /// 文本构造器
    /// </summary>
    private static readonly StringBuilder s_TextBuilder = new StringBuilder();

    /// <summary>
    /// 超链接正则
    /// </summary>
    private static readonly Regex s_HrefRegex =
        new Regex(@"<a href=([^>\n\s]+)>(.*?)(</a>)", RegexOptions.Singleline);

    [Serializable]
    public class HrefClickEvent : UnityEvent<string> { }

    [SerializeField]
    private HrefClickEvent m_OnHrefClick = new HrefClickEvent();

    /// <summary>
    /// 超链接点击事件
    /// </summary>
    public HrefClickEvent onHrefClick
    {
        get { return m_OnHrefClick; }
        set { m_OnHrefClick = value; }
    }

    /// <summary>
    /// 获取超链接解析后的最后输出文本
    /// </summary>
    /// <returns></returns>
    protected string GetOutputText()
    {
        s_TextBuilder.Length = 0;
        m_HrefInfos.Clear();
        var indexText = 0;
        foreach (Match match in s_HrefRegex.Matches(text))
        {
            s_TextBuilder.Append(text.Substring(indexText, match.Index - indexText));
            s_TextBuilder.Append("<color=blue>");  // 超链接颜色

            var group = match.Groups[1];
            var hrefInfo = new HrefInfo
            {
                startIndex = s_TextBuilder.Length * 4, // 超链接里的文本起始顶点索引
                endIndex = (s_TextBuilder.Length + match.Groups[2].Length - 1) * 4 + 3,
                name = group.Value
            };
            m_HrefInfos.Add(hrefInfo);

            s_TextBuilder.Append(match.Groups[2].Value);
            s_TextBuilder.Append("</color>");
            indexText = match.Index + match.Length;
        }
        s_TextBuilder.Append(text.Substring(indexText, text.Length - indexText));
        return s_TextBuilder.ToString();
    }

    /// <summary>
    /// 点击事件检测是否点击到超链接文本
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 lp;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out lp);

        foreach (var hrefInfo in m_HrefInfos)
        {
            var boxes = hrefInfo.boxes;
            for (var i = 0; i < boxes.Count; ++i)
            {
                if (boxes[i].Contains(lp))
                {
                    m_OnHrefClick.Invoke(hrefInfo.name);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 超链接信息类
    /// </summary>
    private class HrefInfo
    {
        public int startIndex;
        public int endIndex;
        public string name;
        public readonly List<Rect> boxes = new List<Rect>();
    }
}

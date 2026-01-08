using UnityEngine;
namespace CoreBit.DestructibleSprite
{
    [System.Serializable]
    public class SelectionLayer
    {
        public Texture2D texture;
        public Rect rect;
        public bool visible = true;
        public float mass = 1f;
        public string name = "";
    }
    public enum SelectionMode { Rectangle, Polygon }
}
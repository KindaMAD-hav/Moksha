using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace CoreBit.DestructibleSprite
{
    public class DestructibleSpriteEditor : EditorWindow
    {
        private Texture2D selectedTexture;
        private Vector2 textureScroll;
        private Rect textureRect;

        private List<Vector2> currentPolygon = new List<Vector2>();
        private bool useRectangleMode = true;
        private int selectedLayerIndex = -1;
        private Vector2 dragStartPos;
        private Rect currentRectSelection;
        private bool isDragging = false;
        private bool dropDownIsOpen = false;
        byte[] originalTextureByte;
        private const float SNAP_DISTANCE = 8f;
        private Vector2 currentMousePos;
        private Texture2D checkerboardTexture;
        private List<Rect> rectangleSelections = new List<Rect>();
        private List<List<Vector2>> polygonSelections = new List<List<Vector2>>();
        private List<SelectionLayer> selectionLayers = new List<SelectionLayer>();
        [MenuItem("Tools/Destructible Sprite Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DestructibleSpriteEditor>("Destructible Sprite Editor");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("💀 Destructible Sprite Editor", titleStyle);
            GUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Sprite Texture", EditorStyles.miniBoldLabel);
            selectedTexture = (Texture2D)EditorGUILayout.ObjectField(selectedTexture, typeof(Texture2D), false);

            if (selectedTexture != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedTexture);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    if (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.isReadable = true;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                    }
                }

                if (originalTextureByte == null)
                {
                    originalTextureByte = selectedTexture.EncodeToPNG();
                }
            }
            else
            {
                originalTextureByte = null;
            }


            GUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck())
            {
                polygonSelections.Clear();
                currentPolygon.Clear();
                rectangleSelections.Clear();
                selectionLayers.Clear();
            }

            if (selectedTexture != null)
            {
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();

                // Left: "Select Mode" label
                GUILayout.Label("Select Mode:", GUILayout.Width(90));

                // Middle: Custom Dropdown stretched to fill space until button
                string[] modes = new string[] { "Rectangle", "Polygon" };
                int selectedModeIndex = useRectangleMode ? 0 : 1;

                GUIStyle customPopupStyle = new GUIStyle("Button")
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                // Custom dropdown button
                Rect dropdownRect = GUILayoutUtility.GetRect(new GUIContent(modes[selectedModeIndex] + " ▼"), customPopupStyle, GUILayout.Height(28), GUILayout.ExpandWidth(true));
                if (GUI.Button(dropdownRect, modes[selectedModeIndex] + " ▼", customPopupStyle))
                {
                    if (!dropDownIsOpen)
                    {
                        CustomDropdown.Show(dropdownRect, modes, selectedModeIndex, (index) =>
                        {
                            selectedModeIndex = index;
                            useRectangleMode = selectedModeIndex == 0;
                            Repaint();
                        });
                    }
                   
                    dropDownIsOpen = !dropDownIsOpen;
                }

                useRectangleMode = selectedModeIndex == 0;

                GUIStyle buttonStyle = new GUIStyle("Button")
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                if (GUILayout.Button("✂ Auto Slice", buttonStyle, GUILayout.Height(28), GUILayout.Width(120)))
                {
                    AutoSlicePopup.ShowWindow(
                        (horizontalCount, verticalCount) =>
                        {
                            AutoSlice(horizontalCount, verticalCount);
                        }
                    );
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(4);

                EditorGUILayout.HelpBox(
                    "- Rectangle: Draw rectangular selections on the sprite.\n" +
                    "- Polygon: Draw polygonal selections for more complex shapes.\n" +
                    "- Auto Slice Button: Automatically slices the sprite into a grid based on the provided horizontal and vertical count.",
                    MessageType.Info
                );

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical(GUILayout.Width(position.width * 0.68f));
                DrawTextureArea();
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.32f));

                GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13
                };
                GUILayout.Label("🧱 Selection Layers", headerStyle);
                GUILayout.Space(2);

                layerScrollPos = GUILayout.BeginScrollView(layerScrollPos);
                int layerToDelete = -1;

                for (int i = 0; i < selectionLayers.Count; i++)
                {
                    SelectionLayer layer = selectionLayers[i];
                    bool isSelected = (i == selectedLayerIndex);

                    Color bgColor = isSelected ? new Color(0.25f, 0.5f, 0.9f, 0.3f) : new Color(0.15f, 0.15f, 0.15f, 0.25f);
                    GUIStyle cardStyle = new GUIStyle(GUI.skin.box)
                    {
                        normal = { background = MakeTexture(1, 1, bgColor) },
                        margin = new RectOffset(4, 4, 4, 4),
                    };

                    GUILayout.BeginHorizontal(cardStyle, GUILayout.Height(70));

                    GUILayout.BeginVertical(GUILayout.Width(28));
                    GUILayout.FlexibleSpace();
                    Texture eyeIcon = layer.visible
                        ? EditorGUIUtility.IconContent("animationvisibilitytoggleon").image
                        : EditorGUIUtility.IconContent("animationvisibilitytoggleoff").image;
                    if (GUILayout.Button(new GUIContent(eyeIcon, "Toggle Visibility"), GUIStyle.none, GUILayout.Width(22), GUILayout.Height(22)))
                    {
                        layer.visible = !layer.visible;
                        Repaint();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(GUILayout.Width(56), GUILayout.Height(56));
                    Rect previewRect = GUILayoutUtility.GetRect(48, 48, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                    GUI.DrawTexture(previewRect, GetCheckerboardTexture(), ScaleMode.ScaleAndCrop);
                    if (layer.texture != null)
                        GUI.DrawTexture(previewRect, layer.texture, ScaleMode.ScaleToFit, true);
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    GUIStyle nameStyle = new GUIStyle(EditorStyles.textField)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 12
                    };
                    layer.name = EditorGUILayout.TextField(string.IsNullOrEmpty(layer.name) ? $"Layer {i + 1}" : layer.name, nameStyle, GUILayout.Width(110));

                    GUIStyle massStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 13,
                        margin = new RectOffset(4, 4, 4, 4),
                        padding = new RectOffset(6, 6, 6, 6)
                    };

                    GUILayout.BeginHorizontal(massStyle);

                    // Label
                    GUILayout.Label("Mass", massStyle, GUILayout.Width(50));

                    // Input Style
                    GUIStyle massInputStyle = new GUIStyle(EditorStyles.numberField)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 13,
                        margin = new RectOffset(4, 4, 4, 4),
                        padding = new RectOffset(6, 6, 6, 6)
                    };
                    massInputStyle.normal.textColor = Color.white;
                    massInputStyle.focused.textColor = Color.white;
                    massInputStyle.hover.textColor = Color.white;
                    massInputStyle.active.textColor = Color.white;
                    // Float Field
                    layer.mass = EditorGUILayout.FloatField(layer.mass, massInputStyle, GUILayout.Width(60), GUILayout.Height(27));

                    GUILayout.EndHorizontal();


                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();

                    // Delete
                    GUILayout.BeginVertical(GUILayout.Width(28));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("✖", GUILayout.Width(22), GUILayout.Height(22)))
                    {
                        RestoreLayerToSource(layer);
                        layerToDelete = i;
                        Repaint();
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                }


                GUILayout.EndScrollView();
                GUILayout.EndVertical(); // right panel
                GUILayout.EndHorizontal();

                // Remove after loop
                if (layerToDelete >= 0)
                {
                    selectionLayers.RemoveAt(layerToDelete);
                    if (selectedLayerIndex == layerToDelete) selectedLayerIndex = -1;
                    else if (selectedLayerIndex > layerToDelete) selectedLayerIndex--;
                    Repaint();
                }

                GUILayout.Space(2);

                // Bottom Action Buttons
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = useRectangleMode
                    ? currentRectSelection.width > 2f && currentRectSelection.height > 2f
                    : currentPolygon.Count >= 3 && currentPolygon.First() == currentPolygon.Last();

                if (GUILayout.Button("➕ Add Selection", GUILayout.Height(25))) { AddSelection(); }
                if (GUILayout.Button("✖ Clear Current", GUILayout.Height(25))) { ClearCurrentSelection(); }
                GUI.enabled = true;

                GUI.enabled = selectionLayers.Count > 0;
                if (GUILayout.Button("🗑 Clear All", GUILayout.Height(25))) { ClearAllSelections(); }
                if (GUILayout.Button("📤 Export All", GUILayout.Height(25)))
                {
                    BreakableItemExportWindow.ShowWindow(
                           (explosionForce, breakDirection, canFadeOut, fadeOutDelay, fadeOutDuration, enableReset, resetDelay, exportOnlyVisibleLayers, ignoreInternalCollisions, hitsToBreak) =>
                           {
                               ExportAllSelectionLayersAsPrefab(explosionForce, breakDirection, canFadeOut, fadeOutDelay, fadeOutDuration, enableReset, resetDelay, exportOnlyVisibleLayers, ignoreInternalCollisions, hitsToBreak);
                           }
                       );

                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(16);

            }
        }
        private void AutoSlice(int horizontalPieces, int verticalPieces)
        {
            if (selectedTexture == null) return;

            selectedTexture = MakeTextureReadable(selectedTexture);

            int pieceWidth = selectedTexture.width / horizontalPieces;
            int pieceHeight = selectedTexture.height / verticalPieces;

            for (int y = 0; y < verticalPieces; y++)
            {
                for (int x = 0; x < horizontalPieces; x++)
                {
                    // Texture Space Rect
                    Rect sliceRect = new Rect(
                        x * pieceWidth,
                        y * pieceHeight,
                        pieceWidth,
                        pieceHeight
                    );

                    Texture2D cut = CutOutRectFromTexture(selectedTexture, sliceRect, true);

                    selectionLayers.Add(new SelectionLayer
                    {
                        texture = cut,
                        rect = sliceRect,
                        visible = true
                    });
                }
            }

            Repaint();
        }




        private void AddSelection()
        {
            if (useRectangleMode)
            {
                if (currentRectSelection.width > 0 && currentRectSelection.height > 0)
                {
                    rectangleSelections.Add(currentRectSelection);
                    selectedTexture = MakeTextureReadable(selectedTexture);
                    Texture2D cut = CutOutRectFromTexture(selectedTexture, currentRectSelection);

                    selectionLayers.Add(new SelectionLayer
                    {
                        texture = cut,
                        rect = currentRectSelection,
                        visible = true
                    });

                    currentRectSelection = Rect.zero;
                    Repaint();
                }
            }
            else
            {
                if (currentPolygon.Count >= 3)
                {
                    List<Vector2> texturePoly = ConvertGuiPolygonToTextureCoords(currentPolygon, textureRect, selectedTexture);

                    selectedTexture = MakeTextureReadable(selectedTexture);
                    Texture2D cut = CutOutPolygonFromTexture(selectedTexture, texturePoly);

                    selectionLayers.Add(new SelectionLayer
                    {
                        texture = cut,
                        rect = new Rect(0, 0, cut.width, cut.height),
                        visible = true
                    });

                    currentPolygon.Clear();
                    Repaint();
                }
            }

        }
        private void ClearCurrentSelection()
        {
            currentPolygon.Clear();
            currentRectSelection = Rect.zero;
            isDragging = false;
            Repaint();
        }
        private void ClearAllSelections()
        {
            foreach (var layer in selectionLayers)
            {
                RestoreLayerToSource(layer);
            }

            polygonSelections.Clear();
            currentPolygon.Clear();
            rectangleSelections.Clear();
            selectionLayers.Clear();
            isDragging = false;
            Repaint();
        }
        private void RestoreLayerToSource(SelectionLayer layer)
        {
            if (selectedTexture == null || layer.texture == null)
                return;

            selectedTexture = MakeTextureReadable(selectedTexture);

            int width = selectedTexture.width;
            int height = selectedTexture.height;

            Color[] destPixels = selectedTexture.GetPixels(0, 0, width, height);
            Color[] srcPixels = layer.texture.GetPixels(0, 0, width, height);

            Color[] outPixels = new Color[width * height];

            for (int i = 0; i < outPixels.Length; i++)
            {
                Color src = srcPixels[i];
                Color dst = destPixels[i];

                float aS = src.a;
                float aD = dst.a;

                float outA = aS + aD * (1f - aS);
                Color outRGB = (src * aS) + (dst * (1f - aS));

                outPixels[i] = new Color(outRGB.r, outRGB.g, outRGB.b, outA);
            }

            selectedTexture.SetPixels(0, 0, width, height, outPixels);
            selectedTexture.Apply(false, false);
        }


        private void ExportAllSelectionLayersAsPrefab(float explosionForce, BreakDirection breakDirection, bool canFadeOut, float fadeOutDelay, float fadeOutDuration, bool enableReset, float resetDelay, bool exportOnlyVisibleLayers, bool ignoreInternalCollisions, int hitsToBreak)
        {
            string folderPath = EditorUtility.SaveFolderPanel("Select Export Folder", "Assets", "");

            if (string.IsNullOrEmpty(folderPath))
                return;

            folderPath = FileUtil.GetProjectRelativePath(folderPath);

            SpriteRenderer mainSpriteRenderer = null;
            GameObject root = new GameObject("ExportedLayers");
            BreakableItemLayer[] breakableItemLayer = new BreakableItemLayer[selectionLayers.Count];

            if (selectedTexture != null)
            {
                string mainFileName = "main_sprite.png";
                string mainPngPath = System.IO.Path.Combine(folderPath, mainFileName);

                System.IO.File.WriteAllBytes(mainPngPath, originalTextureByte);

                string mainAssetPath = mainPngPath.Replace("\\", "/");
                AssetDatabase.ImportAsset(mainAssetPath, ImportAssetOptions.ForceUpdate);
                TextureImporter mainImporter = AssetImporter.GetAtPath(mainAssetPath) as TextureImporter;

                if (mainImporter != null)
                {
                    mainImporter.textureType = TextureImporterType.Sprite;
                    mainImporter.spriteImportMode = SpriteImportMode.Single;
                    mainImporter.SaveAndReimport();
                }

                Sprite mainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(mainAssetPath);

                GameObject mainObj = new GameObject("MainSprite");
                mainObj.transform.SetParent(root.transform);

                mainSpriteRenderer = mainObj.AddComponent<SpriteRenderer>();
                mainSpriteRenderer.sprite = mainSprite;

                var poly = mainObj.AddComponent<PolygonCollider2D>();
                poly.isTrigger = false;
            }

            for (int i = 0; i < selectionLayers.Count; i++)
            {
                breakableItemLayer[i] = new BreakableItemLayer();
                var layer = selectionLayers[i];
                layer.visible = true;

                if (exportOnlyVisibleLayers)
                {
                    if (!layer.visible)
                    {
                        continue;
                    }
                }
                if (layer.texture == null)
                    continue;

                string fileName = $"cut_layer_{i}.png";
                string pngPath = System.IO.Path.Combine(folderPath, fileName);
                byte[] pngData = layer.texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(pngPath, pngData);

                string assetPath = System.IO.Path.Combine(folderPath, fileName).Replace("\\", "/");
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                GameObject layerObj = new GameObject($"Layer_{i}");
                layerObj.transform.SetParent(root.transform);
                layerObj.SetActive(false);
                var sr = layerObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                breakableItemLayer[i].SpriteRenderer = sr;

                // Collider
                var poly = layerObj.AddComponent<PolygonCollider2D>();
                poly.isTrigger = false;
                poly.enabled = false;
                breakableItemLayer[i].Collider = poly;

                // Rigidbody
                var rigidbody = layerObj.AddComponent<Rigidbody2D>();
                rigidbody.simulated = false;
                rigidbody.gravityScale = 4;
                breakableItemLayer[i].Rigidbody2D = rigidbody;
                breakableItemLayer[i].Mass = layer.mass;
            }

            AssetDatabase.Refresh();

            string prefabPath = folderPath + "/ExportedLayers.prefab";
            prefabPath = prefabPath.Replace("\\", "/");
            root.AddComponent<CircleCollider2D>().isTrigger = true;
            BreakableItem breakableItem = root.AddComponent<BreakableItem>();

            breakableItem.Layers = breakableItemLayer;
            breakableItem.MainSpriteRenderer = mainSpriteRenderer;
            breakableItem.ExplosionForce = explosionForce;
            breakableItem.BreakDirection = breakDirection;
            breakableItem.EnableFadeOut = canFadeOut;
            breakableItem.IgnoreInternalCollisions = ignoreInternalCollisions;
            breakableItem.HitsToBreak = hitsToBreak;
            breakableItem.FadeOutDelay = fadeOutDelay;
            breakableItem.FadeOutDuration = fadeOutDuration;
            breakableItem.EnableReset = enableReset;
            breakableItem.ResetDelay = resetDelay;
            GameObject prefabGameObject = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"✅ Exported {selectionLayers.Count} layers as prefab at {prefabPath}");

            GameObject.DestroyImmediate(root);
        }

        private bool PointInPolygon(Vector2 p, List<Vector2> poly)
        {
            int n = poly.Count;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private Texture2D CutOutPolygonFromTexture(Texture2D sourceTex, List<Vector2> polygonPoints)
        {
            int width = sourceTex.width;
            int height = sourceTex.height;

            Texture2D cutTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color[] transparentPixels = Enumerable.Repeat(new Color(0, 0, 0, 0), width * height).ToArray();
            cutTex.SetPixels(transparentPixels);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x, y);

                    if (PointInPolygon(p, polygonPoints))
                    {
                        Color col = sourceTex.GetPixel(x, y);
                        cutTex.SetPixel(x, y, col);

                        sourceTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            cutTex.Apply();
            sourceTex.Apply();

            return cutTex;
        }



        private List<Vector2> ConvertGuiPolygonToTextureCoords(List<Vector2> guiPolygon, Rect textureRect, Texture2D texture)
        {
            List<Vector2> texturePolygon = new List<Vector2>();

            float texWidth = texture.width;
            float texHeight = texture.height;

            foreach (var p in guiPolygon)
            {
                float x = (p.x - textureRect.x) * texWidth / textureRect.width;
                float y = (textureRect.height - (p.y - textureRect.y)) * texHeight / textureRect.height;

                texturePolygon.Add(new Vector2(x, y));
            }

            return texturePolygon;
        }
        private Texture2D CutOutRectFromTexture(Texture2D source, Rect rect, bool isTextureSpace = false)
        {
            if (!source.isReadable)
            {
                Debug.LogError("Source texture is not readable!");
                return null;
            }

            int texWidth = source.width;
            int texHeight = source.height;

            int texX, texY, cutWidth, cutHeight;

            if (isTextureSpace)
            {
                texX = Mathf.Clamp(Mathf.FloorToInt(rect.x), 0, texWidth - 1);
                texY = Mathf.Clamp(Mathf.FloorToInt(rect.y), 0, texHeight - 1);
                cutWidth = Mathf.Clamp(Mathf.FloorToInt(rect.width), 1, texWidth - texX);
                cutHeight = Mathf.Clamp(Mathf.FloorToInt(rect.height), 1, texHeight - texY);
            }
            else
            {
                float texelPerPixelX = (float)source.width / textureRect.width;
                float texelPerPixelY = (float)source.height / textureRect.height;

                texX = Mathf.Clamp(Mathf.FloorToInt((rect.x - textureRect.x) * texelPerPixelX), 0, texWidth - 1);
                texY = Mathf.Clamp(Mathf.FloorToInt((textureRect.height - (rect.y - textureRect.y) - rect.height) * texelPerPixelY), 0, texHeight - 1);

                cutWidth = Mathf.Clamp(Mathf.FloorToInt(rect.width * texelPerPixelX), 1, texWidth - texX);
                cutHeight = Mathf.Clamp(Mathf.FloorToInt(rect.height * texelPerPixelY), 1, texHeight - texY);
            }

            Color[] regionPixels = source.GetPixels(texX, texY, cutWidth, cutHeight);

            Texture2D fullSizeCut = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);

            Color[] transparentPixels = new Color[texWidth * texHeight];
            for (int i = 0; i < transparentPixels.Length; i++)
                transparentPixels[i] = new Color(0, 0, 0, 0);
            fullSizeCut.SetPixels(transparentPixels);

            fullSizeCut.SetPixels(texX, texY, cutWidth, cutHeight, regionPixels);
            fullSizeCut.Apply(false, false);

            Color[] clearPixels = Enumerable.Repeat(new Color(0, 0, 0, 0), cutWidth * cutHeight).ToArray();
            source.SetPixels(texX, texY, cutWidth, cutHeight, clearPixels);
            source.Apply(false, false);

            return fullSizeCut;
        }


        private Texture2D MakeTextureReadable(Texture2D source)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default); //

            Graphics.Blit(source, tmp);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = tmp;

            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0, false);
            readable.Apply(false, false);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);

            return readable;
        }

        private Vector2 layerScrollPos;

        private void DrawTextureArea()
        {
            float maxPreviewWidth = position.width * 0.7f - 20;
            float maxPreviewHeight = position.height - 200;

            float scale = Mathf.Min(maxPreviewWidth / selectedTexture.width, maxPreviewHeight / selectedTexture.height);
            Vector2 previewSize = new Vector2(selectedTexture.width * scale, selectedTexture.height * scale);
            textureRect = GUILayoutUtility.GetRect(previewSize.x, previewSize.y, GUILayout.ExpandWidth(false));

            EditorGUI.DrawRect(textureRect, Color.black);
            GUI.DrawTexture(textureRect, selectedTexture, ScaleMode.ScaleToFit);

            Event e = Event.current;
            if (!useRectangleMode && textureRect.Contains(e.mousePosition))
            {
                currentMousePos = e.mousePosition;
                Repaint();
            }

            if (useRectangleMode)
                HandleRectInput(textureRect);
            else
                HandlePolygonInput(textureRect);





            if (useRectangleMode && currentRectSelection.width > 0 && currentRectSelection.height > 0)
            {
                Handles.DrawSolidRectangleWithOutline(currentRectSelection, new Color(1, 1, 0, 0.2f), Color.yellow);
            }
            else if (!useRectangleMode && currentPolygon.Count > 0)
            {
                DrawPolygon(currentPolygon, new Color(1f, 1f, 0f, 0.2f), Color.yellow, true, currentMousePos);
            }


            foreach (var layer in selectionLayers)
            {
                if (!layer.visible || layer.texture == null)
                    continue;

                GUI.DrawTexture(textureRect, layer.texture, ScaleMode.ScaleToFit, true);
            }
        }


        private void HandleRectInput(Rect drawRect)
        {
            Event e = Event.current;
            if (!drawRect.Contains(e.mousePosition)) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                dragStartPos = e.mousePosition;
                isDragging = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDragging)
            {
                Vector2 dragEndPos = e.mousePosition;
                currentRectSelection = GetScreenRect(dragStartPos, dragEndPos);
                Repaint();
            }
            else if (e.type == EventType.MouseUp && isDragging)
            {
                Vector2 dragEndPos = e.mousePosition;
                currentRectSelection = GetScreenRect(dragStartPos, dragEndPos);
                isDragging = false;
                Repaint();
            }
        }

        private void HandlePolygonInput(Rect drawRect)
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            if (drawRect.Contains(mousePos))
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    Vector2 snapped = TrySnapToExisting(mousePos);

                    if (currentPolygon.Count > 0 && currentPolygon.Count >= 3 && Vector2.Distance(snapped, currentPolygon[0]) <= SNAP_DISTANCE)
                    {
                        currentPolygon.Add(currentPolygon[0]);
                    }
                    else
                    {
                        currentPolygon.Add(snapped);
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
                {
                    currentMousePos = mousePos;
                    Repaint();
                }
            }
        }

        private Vector2 TrySnapToExisting(Vector2 point)
        {
            foreach (var existing in currentPolygon)
            {
                if (Vector2.Distance(point, existing) <= SNAP_DISTANCE)
                    return existing;
            }
            return point;
        }

        private static Material _glFillMaterial;

        private void DrawPolygon(List<Vector2> points, Color fill, Color outline, bool drawPreviewLine = false, Vector2? mousePos = null)
        {
            if (points.Count == 0) return;

            if (points.Count == 1)
            {
                if (drawPreviewLine && mousePos.HasValue)
                {
                    Handles.color = outline;
                    Handles.DrawLine(points[0], mousePos.Value);
                }

                Handles.color = Color.green;
                Handles.DrawSolidDisc(points[0], Vector3.forward, 4f);
                return;
            }

            bool isClosed = points.Count >= 3 && points.First() == points.Last();

            Vector3[] worldPoints = points.Select(p => new Vector3(p.x, p.y, 0f)).ToArray();

            if (isClosed)
            {
                List<Vector2> polygonPoints = points;
                if (points.First() == points.Last())
                    polygonPoints = points.Take(points.Count - 1).ToList();

                List<int> indices = TriangulatePolygon(polygonPoints);

                if (!IsPolygonClockwise(polygonPoints))
                {
                    polygonPoints.Reverse();
                }

                if (indices.Count >= 3)
                {
                    Handles.BeginGUI();

                    if (_glFillMaterial == null)
                    {
                        Shader shader = Shader.Find("Hidden/Internal-Colored");
                        _glFillMaterial = new Material(shader)
                        {
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        _glFillMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        _glFillMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        _glFillMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        _glFillMaterial.SetInt("_ZWrite", 0);
                    }

                    _glFillMaterial.SetPass(0);

                    GL.PushMatrix();
                    GL.MultMatrix(Matrix4x4.identity);
                    GL.Begin(GL.TRIANGLES);
                    GL.Color(fill);

                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        int i0 = indices[i];
                        int i1 = indices[i + 1];
                        int i2 = indices[i + 2];

                        GL.Vertex(new Vector3(polygonPoints[i0].x, polygonPoints[i0].y, 0f));
                        GL.Vertex(new Vector3(polygonPoints[i1].x, polygonPoints[i1].y, 0f));
                        GL.Vertex(new Vector3(polygonPoints[i2].x, polygonPoints[i2].y, 0f));
                    }

                    GL.End();
                    GL.PopMatrix();
                    Handles.EndGUI();
                }

                Handles.color = outline;
                Handles.DrawAAPolyLine(2f, worldPoints);
            }
            else
            {
                Handles.color = outline;
                Handles.DrawAAPolyLine(2f, worldPoints);

                if (drawPreviewLine && mousePos.HasValue && points.Count > 0)
                {
                    Handles.color = outline;
                    Handles.DrawLine(points.Last(), mousePos.Value);
                }

                if (points.Count > 0 && mousePos.HasValue)
                {
                    float distToStart = Vector2.Distance(points[0], mousePos.Value);
                    if (distToStart <= SNAP_DISTANCE)
                    {
                        Handles.color = Color.red;
                        Handles.DrawSolidDisc(points[0], Vector3.forward, 6f);
                    }
                    else
                    {
                        Handles.color = Color.green;
                        Handles.DrawSolidDisc(points[0], Vector3.forward, 4f);
                    }
                }
            }
        }
        private bool IsPolygonClockwise(List<Vector2> points)
        {
            float sum = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                sum += (p2.x - p1.x) * (p2.y + p1.y);
            }
            return sum > 0f;
        }
        private List<int> TriangulatePolygon(List<Vector2> points)
        {
            Triangulator triangulator = new Triangulator(points.ToArray());
            return triangulator.Triangulate().ToList();
        }

        private Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            return new Rect(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Abs(start.x - end.x),
                Mathf.Abs(start.y - end.y)
            );
        }
        private Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        private Texture2D GetCheckerboardTexture()
        {
            if (checkerboardTexture != null)
                return checkerboardTexture;

            int size = 16;
            checkerboardTexture = new Texture2D(size, size);
            Color light = new Color(0.75f, 0.75f, 0.75f);
            Color dark = new Color(0.55f, 0.55f, 0.55f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isDark = (x / 4 + y / 4) % 2 == 0;
                    checkerboardTexture.SetPixel(x, y, isDark ? dark : light);
                }
            }

            checkerboardTexture.wrapMode = TextureWrapMode.Repeat;
            checkerboardTexture.filterMode = FilterMode.Point;
            checkerboardTexture.Apply();

            return checkerboardTexture;
        }

    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyGiantStudio.DevTools
{
    public class Header
    {
        DevToolsWindow _devToolsWindow;

        const string CompanyAssetStoreLink = "https://assetstore.unity.com/publishers/45848?aid=1011ljxWe";
        const string AssetLink = "https://assetstore.unity.com/packages/slug/291626?aid=1011ljxWe";

        GroupBox _assetInformationGroupBox;

        public ToolbarSearchField SearchBar;

        Button _companyButton;

        List<VisualElement> _icons = new();

        public Header(VisualElement root, DevToolsWindow devToolsWindow)
        {
            Setup(root, devToolsWindow);
        }

        /// <summary>
        /// Called once when this script instance is created
        /// </summary>
        void Setup(VisualElement root, DevToolsWindow devToolsWindow)
        {
            GroupBox groupBox = root.Q<GroupBox>("HeaderRoot");
            _devToolsWindow = devToolsWindow;

            SetupAssetInformation(groupBox);

            SetupTabs(groupBox, devToolsWindow);

            SearchBar = groupBox.Q<ToolbarSearchField>("SearchBar");
            string originalSearchingFor = devToolsWindow.SearchManger.SearchingFor();
            SearchBar.value = originalSearchingFor;
            SearchBar.RegisterValueChangedCallback((evt) =>
            {
                devToolsWindow.SearchManger.SetNewSearch(evt.newValue);
                devToolsWindow.UpdatePages();
            });

            groupBox.RegisterCallback<GeometryChangedEvent>(evt => { Adapt(evt.newRect.width); });
        }

        void SetupAssetInformation(GroupBox groupBox)
        {
            _assetInformationGroupBox = groupBox.Q<GroupBox>("AssetInformation");

            Button assetIconButton = groupBox.Q<Button>("AssetIconButton");
            assetIconButton.clicked += () => { Application.OpenURL(AssetLink); };
            Button assetNameButton = groupBox.Q<Button>("AssetNameButton");
            assetNameButton.clicked += () => { Application.OpenURL(AssetLink); };

            _companyButton = groupBox.Q<Button>("CompanyButton");
            _companyButton.clicked += () => { Application.OpenURL(CompanyAssetStoreLink); };
        }

        List<Label> _tabNames = new();

        void SetupTabs(GroupBox groupBox, DevToolsWindow devToolsWindow)
        {
            VisualTreeAsset tabAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Plugins/Tiny Giant Studio/DevTools/Common/Scripts/Editor/Header/TabTemplate.uxml");

            GroupBox tabHolder = groupBox.Q<GroupBox>("Tabs");
            tabHolder.Clear(); //Just in-case

            _tabNames ??= new();
            _tabNames.Clear();

            List<TabPage> pages = devToolsWindow.tabPages;
            foreach (TabPage page in pages.Where(page => page != null))
            {
                page.ClosePage();

                VisualElement tab = new();
                tabAsset.CloneTree(tab);
                tabHolder.Add(tab);
                page.tab = tab;

                tab.tooltip = page.tooltip;

                Label nameLabel = tab.Q<Label>("Name");
                nameLabel.text = page.tabName;
                _tabNames.Add(nameLabel);
                VisualElement icon = tab.Q<VisualElement>("Icon");
                icon.AddToClassList(page.tabIcon);
                _icons.Add(icon);

                var b = tab.Q<Button>();
                TabPage page1 = page;
                b.clicked += () => { SelectTab(page1); };
            }
        }

        /// <summary>
        /// Selects the tab page and saves it in settings.
        ///
        /// The alternative method is ResumeTab that doesn't save selected tab
        /// </summary>
        /// <param name="selectedPage"></param>
        public void SelectTab(TabPage selectedPage)
        {
            Settings.instance.selectedTab = selectedPage;
            Settings.instance.selectedPage = null;

            SelectTabBase(selectedPage);
        }

        /// <summary>
        /// Select Tab without changing selected tab, page in settings
        ///
        /// SelectTab method does.
        /// </summary>
        /// <param name="selectedPage"></param>
        public void ResumeTab(TabPage selectedPage)
        {
            SelectTabBase(selectedPage);
        }

        void SelectTabBase(TabPage selectedPage)
        {
            selectedPage.UpdatePage();
            selectedPage.OpenPage();
            selectedPage.tab.AddToClassList("tab-selected");

            foreach (TabPage page in _devToolsWindow.tabPages)
            {
                if (page != selectedPage)
                {
                    page.ClosePage();
                    page.tab?.RemoveFromClassList("tab-selected");
                }
            }
        }

        void Adapt(float width)
        {
            if (width < 350)
            {
                HideLabels();
                ShowIcons();
                _assetInformationGroupBox.style.display = DisplayStyle.None;
            }
            else if (width < 500)
            {
                HideFullTabLabels();
                HideIcons();
                _assetInformationGroupBox.style.display = DisplayStyle.None;
            }
            else if (width < 600)
            {
                ShowFullTabLabels();
                HideIcons();
                _assetInformationGroupBox.style.display = DisplayStyle.None;
            }
            else if (width < 710)
            {
                ShowFullTabLabels();
                HideIcons();
                _assetInformationGroupBox.style.display = DisplayStyle.None;
            }
            else if (width < 815)
            {
                ShowFullTabLabels();
                HideIcons();
                _assetInformationGroupBox.style.display = DisplayStyle.Flex;
            }
            else if (width < 880)
            {
                ShowFullTabLabels();
                _companyButton.text = "Tiny Giant Studio";
                ShowIcons();
                _assetInformationGroupBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                ShowFullTabLabels();
                _companyButton.text = "by Tiny Giant Studio";
                ShowIcons();
                _assetInformationGroupBox.style.display = DisplayStyle.Flex;
            }
        }

        void ShowFullTabLabels()
        {
            List<TabPage> tabPages = _devToolsWindow.tabPages;

            if (tabPages.Count != _tabNames.Count)
                return;

            for (int i = 0; i < _tabNames.Count; i++)
            {
                _tabNames[i].style.display = DisplayStyle.Flex;
                _tabNames[i].text = tabPages[i].tabName;
            }
        }

        void HideFullTabLabels()
        {
            List<TabPage> tabPages = _devToolsWindow.tabPages;

            if (tabPages.Count != _tabNames.Count)
                return;

            for (int i = 0; i < _tabNames.Count; i++)
            {
                _tabNames[i].style.display = DisplayStyle.Flex;
                _tabNames[i].text = tabPages[i].tabShortName;
            }
        }

        void HideLabels()
        {
            List<TabPage> tabPages = _devToolsWindow.tabPages;

            if (tabPages.Count != _tabNames.Count)
                return;

            foreach (Label tabLabel in _tabNames)
            {
                tabLabel.style.display = DisplayStyle.None;
            }
        }

        void ShowIcons()
        {
            foreach (VisualElement icon in _icons)
            {
                icon.style.display = DisplayStyle.Flex;
            }
        }

        void HideIcons()
        {
            foreach (VisualElement icon in _icons)
            {
                icon.style.display = DisplayStyle.None;
            }
        }
    }
}
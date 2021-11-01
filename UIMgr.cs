using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUIWidget
{
    void OnLoad(params object[] args);
}

public class UIMgr : MonoBehaviour
{
    public delegate void OnOpenUIDelegate(bool bSuccess, object param);
    public delegate void OnLoadUIItemDelegate(GameObject resItem, object param1);

    private Transform BaseUIRoot;      // 位于UI最底层，常驻场景，基础交互
    private Transform PopUIRoot;       // 位于UI上层，弹出式，互斥
    private Transform StoryUIRoot;     // 故事背景层
    private Transform TipUIRoot;       // 位于UI顶层，弹出重要提示信息等
    private Transform MenuPopUIRoot;
    private Transform MessageUIRoot;
    private Transform DeathUIRoot;


    private Dictionary<string, GameObject> m_dicTipUI = new Dictionary<string, GameObject>(); //物品信息弹窗
    private Dictionary<string, GameObject> m_dicBaseUI = new Dictionary<string, GameObject>();//主界面所有图标显示
    private Dictionary<string, GameObject> m_dicPopUI = new Dictionary<string, GameObject>();  //弹窗
    private Dictionary<string, GameObject> m_dicStoryUI = new Dictionary<string, GameObject>();//对话框
    private Dictionary<string, GameObject> m_dicMenuPopUI = new Dictionary<string, GameObject>();//菜单
    private Dictionary<string, GameObject> m_dicMessageUI = new Dictionary<string, GameObject>();//消息弹窗
    private Dictionary<string, GameObject> m_dicDeathUI = new Dictionary<string, GameObject>();//死亡
    private Dictionary<string, GameObject> m_dicCacheUI = new Dictionary<string, GameObject>();//缓存

    private Dictionary<string, int> m_dicWaitLoad = new Dictionary<string, int>(); //等待加载列表
    //private static string[] m_MenuBarPopUI = { "RoleView", "PartnerAndMountRoot", "MissionLogRoot", "RelationRoot", "BelleController", "BackPackRoot", "EquipStrengthenRoot", "SkillRoot" };

    private static UIMgr m_instance;
    public static UIMgr Instance()
    {
        return m_instance;
    }
    private const int GCCollectTime = 1;

    void Awake()
    {
        m_dicTipUI.Clear();
        m_dicBaseUI.Clear();
        m_dicPopUI.Clear();
        m_dicStoryUI.Clear();
        m_dicMenuPopUI.Clear();
        m_dicMessageUI.Clear();
        m_dicDeathUI.Clear();
        m_dicCacheUI.Clear();
        m_instance = this;

        //获取组件
        //BaseUIRoot = gameObject.transform.Find("BaseUIRoot");
        //PopUIRoot = gameObject.transform.Find("PopUIRoot");
        //StoryUIRoot = gameObject.transform.Find("StoryUIRoot");
        //TipUIRoot = gameObject.transform.Find("TipUIRoot");
        //MenuPopUIRoot = gameObject.transform.Find("MenuPopUIRoot");
        //MessageUIRoot = gameObject.transform.Find("MessageUIRoot");
        //DeathUIRoot = gameObject.transform.Find("DeathUIRoot");

        //组件初始化
        //BaseUIRoot.gameObject.SetActive(true);
        //TipUIRoot.gameObject.SetActive(true);
        //PopUIRoot.gameObject.SetActive(true);
        //StoryUIRoot.gameObject.SetActive(true);
        //MenuPopUIRoot.gameObject.SetActive(true);
        //MessageUIRoot.gameObject.SetActive(true);
        //DeathUIRoot.gameObject.SetActive(true);
    }


    void OnDestroy()
    {
        m_instance = null;
    }

    //加载ui调用 外部调用
    public static bool LoadItem(UIPathData pathData, OnLoadUIItemDelegate delLoadItem, object param = null)
    {
        if (null == m_instance)
        {
            //单例为空 输出报错
            return false;
        }

        m_instance.LoadUIItem(pathData, delLoadItem, param);
        return true;
    }


    // 加载UI 类内调用
    void LoadUIItem(UIPathData uiData, OnLoadUIItemDelegate delLoadItem, object param = null)
    {
       // GameObject curWindow = ResourceManager.LoadResource("Prefab/UI/" + uiData.name) as GameObject;
        GameObject curWindow = Resources.Load<GameObject>("");// ResourceManager.LoadResource("Prefab/UI/" + uiData.name) as GameObject;

        if (null != curWindow)
        {
            DoLoadUIItem(uiData, curWindow, delLoadItem, param);
            return;
        }

        if (uiData.uiGroupName != null)
        {
            //走ab包加载
           //  GameObject objCacheBundle = BundleManager.GetGroupUIItem(uiData);
            //if (null != objCacheBundle)
            //{
            //    DoLoadUIItem(uiData, objCacheBundle, delLoadItem, param);
            //    return;
            //}
        }

        //如果库中没有保存，走resources读取或ab包
        //StartCoroutine(BundleManager.LoadUI(uiData, DoLoadUIItem, delLoadItem, param));
    }

    void DoLoadUIItem(UIPathData uiData, GameObject curItem, object fun, object param)
    {
        if (null != fun)
        {
            OnLoadUIItemDelegate delLoadItem = fun as OnLoadUIItemDelegate;
            delLoadItem(curItem, param);
        }
    }


    //打开ui
    public static bool ShowUI(UIPathData pathData, OnOpenUIDelegate delOpenUI = null, object param = null)
    {
        if (null == m_instance)
        {
           // LogModule.ErrorLog("game manager is not init");
            return false;
        }
        //查看等待列表中是否有需要加载的，并计数
        m_instance.AddLoadDicRefCount(pathData.name);


        //拿到存储该类ui的容器
        Dictionary<string, GameObject> curDic = null;
        switch (pathData.uiType)
        {
            case UIPathData.UIType.TYPE_BASE:
                curDic = m_instance.m_dicBaseUI;
                break;
            case UIPathData.UIType.TYPE_POP:
                curDic = m_instance.m_dicPopUI;
                break;
            case UIPathData.UIType.TYPE_STORY:
                curDic = m_instance.m_dicStoryUI;
                break;
            case UIPathData.UIType.TYPE_TIP:
                curDic = m_instance.m_dicTipUI;
                break;
            case UIPathData.UIType.TYPE_MENUPOP:
                curDic = m_instance.m_dicMenuPopUI;
                break;
            case UIPathData.UIType.TYPE_MESSAGE:
                curDic = m_instance.m_dicMessageUI;
                break;
            case UIPathData.UIType.TYPE_DEATH:
                curDic = m_instance.m_dicDeathUI;

                break;
            default:
                return false;
        }

        if (null == curDic)
        {
            return false;
        }


        if (m_instance.m_dicCacheUI.ContainsKey(pathData.name))
        {
            //不存在就添加到对应容器中
            if (!curDic.ContainsKey(pathData.name))
            {
                curDic.Add(pathData.name, m_instance.m_dicCacheUI[pathData.name]);
            }
            //从缓存容器中移除
            m_instance.m_dicCacheUI.Remove(pathData.name);
        }

        if (curDic.ContainsKey(pathData.name))
        {
            //打开ui
            curDic[pathData.name].SetActive(true);

            m_instance.DoAddUI(pathData, curDic[pathData.name], delOpenUI, param);
            return true;
        }

        m_instance.LoadUI(pathData, delOpenUI, param);

        return true;
    }

    void DoAddUI(UIPathData uiData, GameObject curWindow, object fun, object param)
    {  
        if (null != curWindow)
        {
            //取到canvas 以及容器
            Transform parentRoot = null;
            Dictionary<string, GameObject> relativeDic = null;
            switch (uiData.uiType)
            {
                case UIPathData.UIType.TYPE_BASE:
                    parentRoot = BaseUIRoot;
                    relativeDic = m_dicBaseUI;
                    break;
                case UIPathData.UIType.TYPE_POP:
                    parentRoot = PopUIRoot;
                    relativeDic = m_dicPopUI;
                    break;
                case UIPathData.UIType.TYPE_STORY:
                    parentRoot = StoryUIRoot;
                    relativeDic = m_dicStoryUI;
                    break;
                case UIPathData.UIType.TYPE_TIP:
                    parentRoot = TipUIRoot;
                    relativeDic = m_dicTipUI;
                    break;
                case UIPathData.UIType.TYPE_MENUPOP:
                    parentRoot = MenuPopUIRoot;
                    relativeDic = m_dicMenuPopUI;
                    break;
                case UIPathData.UIType.TYPE_MESSAGE:
                    parentRoot = MessageUIRoot;
                    relativeDic = m_dicMessageUI;
                    break;
                case UIPathData.UIType.TYPE_DEATH:
                    parentRoot = DeathUIRoot;
                    relativeDic = m_dicDeathUI;
                    break;
                default:
                    break;

            }

            //判断ui类型，执行关闭其他ui
            if (uiData.uiType == UIPathData.UIType.TYPE_POP || uiData.uiType == UIPathData.UIType.TYPE_MENUPOP)
            {
                OnLoadNewPopUI(m_dicPopUI, uiData.name);
                OnLoadNewPopUI(m_dicMenuPopUI, uiData.name);
            }
            if (null != relativeDic && relativeDic.ContainsKey(uiData.name))
            {
                relativeDic[uiData.name].SetActive(true);
            }

            else if (null != parentRoot && null != relativeDic)
            {
                GameObject newWindow = GameObject.Instantiate(curWindow) as GameObject;
                //界面初始化
                if (null != newWindow)
                {
                    Vector3 oldScale = newWindow.transform.localScale;
                    newWindow.transform.parent = parentRoot;
                    newWindow.transform.localPosition = Vector3.zero;
                    newWindow.transform.localScale = oldScale;
                    relativeDic.Add(uiData.name, newWindow);
                    if (uiData.uiType == UIPathData.UIType.TYPE_MENUPOP)
                    {
                        LoadMenuSubUIShield(newWindow);
                    }
                }
            }
            //判断类型，关闭其他ui面板
            if (uiData.uiType == UIPathData.UIType.TYPE_STORY)
            {
                BaseUIRoot.gameObject.SetActive(false);
                TipUIRoot.gameObject.SetActive(false);
                PopUIRoot.gameObject.SetActive(false);
                MenuPopUIRoot.gameObject.SetActive(false);
                MessageUIRoot.gameObject.SetActive(false);
                StoryUIRoot.gameObject.SetActive(true);
            }
            else if (uiData.uiType == UIPathData.UIType.TYPE_MENUPOP)
            {

                //此处为加载角色个人状态栏及头像

            }
            else if (uiData.uiType == UIPathData.UIType.TYPE_DEATH)
            {
                ReliveCloseOtherSubUI();
            }
            else if (uiData.uiType == UIPathData.UIType.TYPE_POP)
            {
                //此处为玩家的头衔信息显示

                if (!(uiData.name.Equals("ServerChooseController") ||
                    uiData.name.Equals("RoleCreate")))
                {
                    // StartCoroutine(GCAfterOneSceond());
                }
            }
        }

        if (null != fun)
        {
            OnOpenUIDelegate delOpenUI = fun as OnOpenUIDelegate;
            delOpenUI(curWindow != null, param);
        }
    }


    //打开ui
    void LoadUI(UIPathData uiData, OnOpenUIDelegate delOpenUI = null, object param1 = null)
    {
        GameObject curWindow = Resources.Load<GameObject>("Prefab/UI/" + uiData.name);//ResourceManager.LoadResource("Prefab/UI/" + uiData.name) as GameObject;
        //对ui进行操作，认父级调整位置等
        if (null != curWindow)
        {
            DoAddUI(uiData, curWindow, delOpenUI, param1);
            //LogModule.ErrorLog("can not open controller path not found:" + path);
            return;
        }
        //换一种加载方式，对ui进行操作，认父级调整位置等
        if (uiData.uiGroupName != null)
        {

            GameObject objCacheBundle = null; //BundleManager.GetGroupUIItem(uiData);
            if (null != objCacheBundle)
            {
                DoAddUI(uiData, objCacheBundle, delOpenUI, param1);
                return;
            }
        }
         //协程加载                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  
       // StartCoroutine(BundleManager.LoadUI(uiData, DoAddUI, delOpenUI, param1));
    }

    void AddLoadDicRefCount(string pathName)
    {
        if (m_dicWaitLoad.ContainsKey(pathName))
        {
            //已存在计数
            m_dicWaitLoad[pathName]++;
        }
        else
        {
            //不存在添加
            m_dicWaitLoad.Add(pathName, 1);
        }
    }

    //打开互斥界面，关闭其他ui
    private void OnLoadNewPopUI(Dictionary<string, GameObject> curList, string curName)
    {
        if (curList == null)
        {
            return;
        }

        List<string> objToRemove = new List<string>();

        if (curList.Count > 0)
        {
            objToRemove.Clear();
            foreach (KeyValuePair<string, GameObject> objs in curList)
            {
                if (curName == objs.Key)
                {
                    continue;
                }
                objs.Value.SetActive(false);
                objToRemove.Add(objs.Key);
                if (UIPathData.m_DicUIName.ContainsKey(objs.Key) && UIPathData.m_DicUIName[objs.Key].isDestroyOnUnload)
                {
                    DestroyUI(objs.Key, objs.Value);
                }
                else
                {
                    //添加到缓存中
                    m_dicCacheUI.Add(objs.Key, objs.Value);
                }
            }
            //清除容器中的ui
            for (int i = 0; i < objToRemove.Count; i++)
            {
                if (curList.ContainsKey(objToRemove[i]))
                {
                    curList.Remove(objToRemove[i]);
                }
            }
        }
    }

    //销毁ui
    void DestroyUI(string name, GameObject obj)
    {
        Destroy(obj);
        //卸载ab包资源 释放内存
        //BundleManager.OnUIDestroy(name);
    }

    //加载ui进行初始化
    static void LoadMenuSubUIShield(GameObject newWindow)
    {
        //加载ui
        GameObject MenuSubUIShield = Resources.Load<GameObject>("Prefab/UI/MenuSubUIShield"); //ResourceManager.InstantiateResource("Prefab/UI/MenuSubUIShield") as GameObject;
        if (MenuSubUIShield == null)
        {
          // LogModule.ErrorLog("can not open MenuSubUIShield path not found");
            return;
        }
        MenuSubUIShield.transform.parent = newWindow.transform;
        MenuSubUIShield.transform.localPosition = Vector3.zero;
        MenuSubUIShield.transform.localScale = Vector3.one;
    }

    // 制造一个空物体并返回
    GameObject AddObjToRoot(string name)
    {
        GameObject obj = new GameObject();
        obj.transform.parent = transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;
        obj.name = name;
        return obj;
    }

    //关闭所有ui
    static void ReliveCloseOtherSubUI()
    {
        // 关闭所有PopUI
        List<string> uiKeyList = new List<string>();
        foreach (KeyValuePair<string, GameObject> pair in m_instance.m_dicPopUI)
        {
            uiKeyList.Add(pair.Key);
        }
        for (int i = 0; i < uiKeyList.Count; i++)
        {
            m_instance.ClosePopUI(uiKeyList[i]);
        }
        uiKeyList.Clear();
        // 关闭所有MenuPopUI
        foreach (KeyValuePair<string, GameObject> pair in m_instance.m_dicMenuPopUI)
        {
            uiKeyList.Add(pair.Key);
        }
        for (int i = 0; i < uiKeyList.Count; i++)
        {
            m_instance.CloseMenuPopUI(uiKeyList[i]);
        }
        uiKeyList.Clear();
        // 关闭所有TipUI
        foreach (KeyValuePair<string, GameObject> pair in m_instance.m_dicTipUI)
        {
            uiKeyList.Add(pair.Key);
        }
        for (int i = 0; i < uiKeyList.Count; i++)
        {
            m_instance.CloseTipUI(uiKeyList[i]);
        }
        uiKeyList.Clear();
        // 关闭所有除CentreNotice以外的MessageUI MessageUIRoot节点保留不隐藏
        foreach (KeyValuePair<string, GameObject> pair in m_instance.m_dicMessageUI)
        {
            if (!pair.Key.Contains("CentreNotice"))
            {
                uiKeyList.Add(pair.Key);
            }
        }
        for (int i = 0; i < uiKeyList.Count; i++)
        {
            m_instance.CloseMessageUI(uiKeyList[i]);
        }
        uiKeyList.Clear();

        // 中断剧情对话
        //if (StoryDialogLogic.Instance() != null)
        //{
        //    CloseUI(UIInfo.StoryDialogRoot);
        //}
        // 中断诗词对话
        //if (ShiCiLogic.Instance() != null)
        //{
        //    CloseUI(UIInfo.ShiCiRoot);
        //}
        // 中断剑谱对话
        //if (JianPuLogic.Instance() != null)
        //{
        //    CloseUI(UIInfo.JianPuRoot);
        //}

        // 隐藏二级UI节点
        m_instance.PopUIRoot.gameObject.SetActive(false);
        m_instance.MenuPopUIRoot.gameObject.SetActive(false);
        m_instance.TipUIRoot.gameObject.SetActive(false);
        m_instance.BaseUIRoot.gameObject.SetActive(false);
    }
    void ClosePopUI(string name)
    {
        OnClosePopUI(m_dicPopUI, name);
    }

    void CloseStoryUI(string name)
    {
        if (TryDestroyUI(m_dicStoryUI, name))
        {
            BaseUIRoot.gameObject.SetActive(true);
            TipUIRoot.gameObject.SetActive(true);
            PopUIRoot.gameObject.SetActive(true);
            MenuPopUIRoot.gameObject.SetActive(true);
            MessageUIRoot.gameObject.SetActive(true);
            StoryUIRoot.gameObject.SetActive(true);
        }
    }

    void CloseBaseUI(string name)
    {
        if (m_dicBaseUI.ContainsKey(name))
        {
            m_dicBaseUI[name].SetActive(false);
        }
    }

    void CloseTipUI(string name)
    {
        TryDestroyUI(m_dicTipUI, name);
    }

    void CloseMenuPopUI(string name)
    {
        OnClosePopUI(m_dicMenuPopUI, name);
    }

    void CloseMessageUI(string name)
    {
        TryDestroyUI(m_dicMessageUI, name);
    }

    void CloseDeathUI(string name)
    {
        if (TryDestroyUI(m_dicDeathUI, name))
        {
            // 关闭复活界面时 恢复节点的显示
            m_instance.PopUIRoot.gameObject.SetActive(true);
            m_instance.MenuPopUIRoot.gameObject.SetActive(true);
            m_instance.TipUIRoot.gameObject.SetActive(true);
            m_instance.BaseUIRoot.gameObject.SetActive(true);
        }
    }

    private void OnClosePopUI(Dictionary<string, GameObject> curList, string curName)
    {
        //if (TryDestroyUI(curList, curName))
        //{
        //    // 关闭导航栏打开的二级UI时 收回导航栏
        //    if (null != PlayerFrameLogic.Instance())
        //    {
        //        PlayerFrameLogic.Instance().gameObject.SetActive(true);
        //        if (PlayerFrameLogic.Instance().Fold)
        //        {
        //            PlayerFrameLogic.Instance().SwitchAllWhenPopUIShow(true);
        //        }
        //    }
        //    if (MenuBarLogic.Instance() != null)
        //    {
        //        if (MenuBarLogic.Instance().Fold)
        //        {
        //            MenuBarLogic.Instance().gameObject.SetActive(true);
        //        }
        //    }
        //}
    }

    private bool TryDestroyUI(Dictionary<string, GameObject> curList, string curName)
    {
        if (curList == null)
        {
            return false;
        }

        if (!curList.ContainsKey(curName))
        {
            return false;
        }

        //#if UNITY_ANDROID

        // < 768M UI不进行缓存
        if (SystemInfo.systemMemorySize < 768)
        {
            DestroyUI(curName, curList[curName]);
            curList.Remove(curName);

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            return true;
        }

        //#endif

        if (UIPathData.m_DicUIName.ContainsKey(curName) && !UIPathData.m_DicUIName[curName].isDestroyOnUnload)
        {
            curList[curName].SetActive(false);
            m_dicCacheUI.Add(curName, curList[curName]);
        }
        else
        {
            DestroyUI(curName, curList[curName]);
        }

        curList.Remove(curName);

        return true;
    }

}

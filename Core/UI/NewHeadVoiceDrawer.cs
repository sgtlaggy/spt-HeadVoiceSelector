#if !UNITY_EDITOR
using EFT.UI;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using EFT;
using EFT.Customization;
using SPT.Reflection.Utils;
using System.Threading.Tasks;
using System.Linq;
using Comfort.Common;
using System.Collections.Generic;
using EFT.InventoryLogic;
using HeadVoiceSelector.Utils;

namespace HeadVoiceSelector.Core.UI
{
    internal class NewVoiceHeadDrawers
    {
        private static bool customizationDrawersCloned = false;
        private static readonly List<EquipmentSlot> _hiddenSlots = new List<EquipmentSlot>
        {
            EquipmentSlot.Earpiece,
            EquipmentSlot.Eyewear,
            EquipmentSlot.FaceCover,
            EquipmentSlot.Headwear
        };
        protected static readonly CompositeDisposable _compositeDisposableClass = new CompositeDisposable();
        private static Dictionary<int, TagBank> _voices = new Dictionary<int, TagBank>();
        private static int _selectedHeadIndex;
        private static int _selectedVoiceIndex;
        // type of PlayerBody.SlotViews
        public static DictionaryListHydra<EquipmentSlot, PlayerBody.SlotView> slotViews;
        // both these GClasses inherit 3672, has ‘Category’ property of type ‘ECustomizationItemCategory’
        private static List<KeyValuePair<MongoID, CustomizationHead>> _headTemplates;
        private static List<KeyValuePair<MongoID, CustomizationPlayerVoice>> _voiceTemplates;
        private static GameObject _overallScreen;

        // Routes to handle server changes
        public static void WTTChangeHead(string id)
        {
            if (id == null)
            {
                Console.WriteLine("Error: id is null.");
                return;
            }

            var response = WebRequestUtils.Post<string>("/WTT/WTTChangeHead", id);
            if (response != null)
            {
                Console.WriteLine("HeadVoiceSelector: Change Head Route has been requested");
            }

        }

        public static void WTTChangeVoice(string id)
        {
            if (id == null)
            {
                Console.WriteLine("Error: id is null.");
                return;
            }
#if DEBUG
            Console.WriteLine($"WTTChangeVoice: id = {id}");
#endif
            var response = WebRequestUtils.Post<string>("/WTT/WTTChangeVoice", id);
            if (response != null)
            {
                Console.WriteLine("'HeadVoiceSelector': Change Voice Route has been requested");
            }
        }



        public static void AddCustomizationDrawers(OverallScreen overallScreen)
        {
            try
            {
                if (customizationDrawersCloned)
                {
#if DEBUG
                    Console.WriteLine("Customization drawers already cloned.");
#endif
                    return;
                }
                else
                {
                    GameObject overallScreenGameobject = overallScreen.gameObject;
                    _overallScreen = overallScreenGameobject;
                    Transform leftSide = overallScreenGameobject.transform.Find("LeftSide");
                    Transform clothingPanel = leftSide.transform.Find("ClothingPanel");

                    if (clothingPanel != null && leftSide != null)
                    {

                        GameObject clonedCustomizationDrawers = GameObject.Instantiate(clothingPanel.gameObject, leftSide);
                        clonedCustomizationDrawers.gameObject.name = "NewHeadVoiceCustomizationDrawers";

                        Vector3 newPosition = clonedCustomizationDrawers.transform.localPosition;
                        newPosition.y -= 50f;
                        clonedCustomizationDrawers.transform.localPosition = newPosition;

                        customizationDrawersCloned = true;

                        if (clonedCustomizationDrawers != null)
                        {
                            Transform headTransform = clonedCustomizationDrawers.transform.Find("Upper");
                            Transform voiceTransform = clonedCustomizationDrawers.transform.Find("Lower");
                            if (headTransform != null && voiceTransform != null)
                            {
                                headTransform.gameObject.name = "Head";
                                voiceTransform.gameObject.name = "Voice";


                                Transform headIconTransform = clonedCustomizationDrawers.transform.Find("Head/Icon");
                                Transform voiceIconTransform = clonedCustomizationDrawers.transform.Find("Voice/Icon");

                                if (headIconTransform != null && voiceIconTransform != null)
                                {
                                    Image headIcon = headIconTransform.GetComponent<Image>();
                                    Image voiceIcon = voiceIconTransform.GetComponent<Image>();

                                    var headIconPng = Path.Combine(HeadVoiceSelector.pluginPath, "WTT-HeadVoiceSelector", "Icons", "icon_face_selector.png");
                                    var voiceIconPng = Path.Combine(HeadVoiceSelector.pluginPath, "WTT-HeadVoiceSelector", "Icons", "icon_voice_selector.png");

                                    if (headIconPng != null && voiceIconPng != null)
                                    {
                                        byte[] headIconByte = File.ReadAllBytes(headIconPng);
                                        byte[] voiceIconByte = File.ReadAllBytes(voiceIconPng);

                                        Texture2D headIcontexture = new Texture2D(2, 2);
                                        Texture2D voiceIcontexture = new Texture2D(2, 2);

                                        ImageConversion.LoadImage(headIcontexture, headIconByte);
                                        ImageConversion.LoadImage(voiceIcontexture, voiceIconByte);

                                        Sprite headIconSprite = Sprite.Create(headIcontexture, new Rect(0, 0, headIcontexture.width, headIcontexture.height), Vector2.zero);
                                        Sprite voiceIconSprite = Sprite.Create(voiceIcontexture, new Rect(0, 0, voiceIcontexture.width, voiceIcontexture.height), Vector2.zero);

                                        headIcon.sprite = headIconSprite;
                                        voiceIcon.sprite = voiceIconSprite;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Couldn't find icons/icon path for new customization dropdowns");
                                    }
                                }

                                Transform headSelectorTransform = clonedCustomizationDrawers.transform.Find("Head/ClothingSelector");
                                Transform voiceSelectorTransform = clonedCustomizationDrawers.transform.Find("Voice/ClothingSelector");
                                if (headSelectorTransform != null && voiceSelectorTransform != null)
                                {
                                    headSelectorTransform.gameObject.name = "HeadSelector";
                                    voiceSelectorTransform.gameObject.name = "VoiceSelector";

                                    DropDownBox headDropDownBox = headSelectorTransform.GetComponent<DropDownBox>();
                                    DropDownBox voiceDropDownBox = voiceSelectorTransform.GetComponent<DropDownBox>();

                                    if (headDropDownBox != null && voiceDropDownBox != null)
                                    {
                                        InitCustomizationDropdowns(headDropDownBox, voiceDropDownBox);
                                        setupCustomizationDrawers(headDropDownBox, voiceDropDownBox);


                                        clonedCustomizationDrawers.gameObject.SetActive(true);

#if DEBUG
                                        Console.WriteLine("Successfully cloned and setup new customization dropdowns!");
#endif
                                    }
                                    else
                                    {
                                        Console.WriteLine("headDropdownBox or voiceDropdownBox is null");
                                    }

                                }
                                else
                                {
                                    Console.WriteLine("headSelectorTransform or voiceSelectorTransform is null");
                                }
                            }
                            else
                            {
                                Console.WriteLine("headTransform or voiceTransform are null");
                            }

                        }
                        else
                        {
                            Console.WriteLine("clonedCustomizationDrawers is null");
                        }
                    }
                    else
                    {
                        Console.WriteLine("customizationDrawersPrefab or overallParent not found");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        public static void InitCustomizationDropdowns(DropDownBox _headSelector, DropDownBox _voiceSelector)
        {
            try
            {

                _compositeDisposableClass.Dispose();

                _compositeDisposableClass.SubscribeEvent<int>(_headSelector.OnValueChanged, new Action<int>(selectHeadEvent));

                _compositeDisposableClass.SubscribeEvent<int>(_voiceSelector.OnValueChanged, new Action<int>(selectVoiceEvent));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during initialization: {ex.Message}");
            }
        }
        public static void setupCustomizationDrawers(DropDownBox _headSelector, DropDownBox _voiceSelector)
        {
            try
            {

                CustomizationSolver instance = Singleton<CustomizationSolver>.Instance;

                if (instance == null)
                {
                    Console.WriteLine("CustomizationSolver instance is null.");
                    return;
                }

                _headTemplates = instance.GetAvailableHeads(PatchConstants.BackEndSession.Profile.Side)
                    .Select((h) => new KeyValuePair<MongoID, CustomizationHead>(h.Id, h)).ToList();
                _voiceTemplates = instance.GetAvailableVoices(PatchConstants.BackEndSession.Profile.Side)
                    .Select((h) => new KeyValuePair<MongoID, CustomizationPlayerVoice>(h.Id, h)).ToList();

#if DEBUG
                Console.WriteLine($"Added {_headTemplates.Count} head customization templates.");
                Console.WriteLine($"Added {_voiceTemplates.Count} voice customization templates.");
#endif
                _voices.Clear();

                if (_headSelector != null)
                {
                    setupHeadDropdownInfo(_headSelector);
                }
                else
                {
                    Console.WriteLine("Head dropdown is null.");
                }

                if (_voiceSelector != null)
                {
                    setupVoiceDropdownInfo(_voiceSelector);
                }
                else
                {
                    Console.WriteLine("Voice dropdown is null.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during customization drawers setup: {ex.Message}");
            }
        }
        public static void setupHeadDropdownInfo(DropDownBox _headSelector)
        {
            try
            {
                var headId = PatchConstants.BackEndSession.Profile.Customization[EBodyModelPart.Head];

                _selectedHeadIndex = _headTemplates.FindIndex(kvp => kvp.Key == headId);
                if (_selectedHeadIndex == -1)
                {
                    var sample = _headTemplates[0].Value;
                    var mockHead = new CustomizationHead
                    {
                        Id = headId,
                        Name = "Unknown",
                        Parent = sample.Parent,
                        Side = sample.Side,
                        _type = sample._type
                    };
                    _headTemplates.Insert(0, new(headId, mockHead));
                    _selectedHeadIndex = 0;
                }

                _headSelector.Show(new Func<IEnumerable<string>>(initializeHeadDropdown), null);
                _headSelector.UpdateValue(_selectedHeadIndex, false, null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during head dropdown info setup: {ex.Message}");
            }
        }
        public static void setupVoiceDropdownInfo(DropDownBox _voiceSelector)
        {
            try
            {
                var voiceId = PatchConstants.BackEndSession.Profile.Customization[EBodyModelPart.Voice];

                _selectedVoiceIndex = _voiceTemplates.FindIndex(kvp => kvp.Key == voiceId);
                if (_selectedVoiceIndex == -1)
                {
                    var sample = _voiceTemplates[0].Value;
                    var mockVoice = new CustomizationPlayerVoice
                    {
                        Id = voiceId,
                        Name = "Unknown",
                        Parent = sample.Parent,
                        Side = sample.Side,
                        _type = sample._type
                    };
                    _voiceTemplates.Insert(0, new(voiceId, mockVoice));
                    _selectedVoiceIndex = 0;
                }

                _voiceSelector.Show(new Func<IEnumerable<string>>(initializeVoiceDropdown), null);
                _voiceSelector.UpdateValue(_selectedVoiceIndex, false, null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during voice dropdown info setup: {ex.Message}");
            }
        }

        public static IEnumerable<string> initializeHeadDropdown()
        {
            return _headTemplates.Select(new Func<KeyValuePair<MongoID, CustomizationHead>, string>(getLocalizedHead)).ToArray<string>();
        }
        public static IEnumerable<string> initializeVoiceDropdown()
        {
            return _voiceTemplates.Select(new Func<KeyValuePair<MongoID, CustomizationPlayerVoice>, string>(getLocalizedVoice)).ToArray<string>();
        }
        public static string getLocalizedHead(KeyValuePair<MongoID, CustomizationHead> x)
        {
#if DEBUG
            Console.WriteLine($"Localizing head: {x.Key}");
#endif
            return x.Value.NameLocalizationKey.Localized(null);
        }
        public static string getLocalizedVoice(KeyValuePair<MongoID, CustomizationPlayerVoice> x)
        {
#if DEBUG
            Console.WriteLine($"Localizing voice: {x.Key}");
#endif
            return x.Value.NameLocalizationKey.Localized(null);
        }
        public static void selectHeadEvent(int selectedIndex)
        {
            try
            {
#if DEBUG
                Console.WriteLine($"Selecting head event for index: {selectedIndex}");
#endif
                if (selectedIndex == _selectedHeadIndex)
                {
#if DEBUG
                    Console.WriteLine("Selected head index is already set.");
#endif
                    return;
                }

                _selectedHeadIndex = selectedIndex;
                string key = _headTemplates[_selectedHeadIndex].Key;
                PatchConstants.BackEndSession.Profile.Customization[EBodyModelPart.Head] = key;
#if DEBUG
                Console.WriteLine($"Head customization updated to: {key}");
#endif
                showPlayerPreview().HandleExceptions();




                WTTChangeHead(key);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during select head event: {ex.Message}");
            }
        }
        public static async Task showPlayerPreview()
        {
            try
            {
                Transform leftSide = _overallScreen.transform.Find("LeftSide");
                Transform characterPanel = leftSide.transform.Find("CharacterPanel");
                PlayerModelView playerModelViewScript = characterPanel.GetComponentInChildren<PlayerModelView>();

                if (leftSide != null)
                {
                    InventoryPlayerModelWithStatsWindow inventoryPlayerModelWithStatsWindow = leftSide.GetComponent<InventoryPlayerModelWithStatsWindow>();

                    if (inventoryPlayerModelWithStatsWindow != null)
                    {
                        await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, new Action(inventoryPlayerModelWithStatsWindow.CG_ShowPreview), 0f, null, true);

                        changeSelectedHead(false, playerModelViewScript);
                    }
                    else
                    {
                        Console.WriteLine("InventoryPlayerModelWithStatsWindow component not found.");
                    }
                }
                else
                {
                    Console.WriteLine("Overall screen parent not found.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during player preview: {ex.Message}");
            }
        }
        public static void changeSelectedHead(bool active, PlayerModelView playerModelView)
        {
            try
            {

                slotViews = playerModelView.PlayerBody.SlotViews;
                foreach (GameObject gameObject in _hiddenSlots.Where(new Func<EquipmentSlot, bool>(getSlotType)).Select(new Func<EquipmentSlot, GameObject>(getSlotKey)).Where(new Func<GameObject, bool>(getModel)))
                {
                    gameObject.SetActive(active);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during change selected head: {ex.Message}");
            }
        }
        public static bool getSlotType(EquipmentSlot slotType)
        {
            return slotViews.ContainsKey(slotType);
        }
        public static GameObject getSlotKey(EquipmentSlot slotType)
        {
            return slotViews.GetByKey(slotType).ParentedModel.Value;
        }
        public static bool getModel(GameObject model)
        {
            return model != null;
        }
        public static void selectVoiceEvent(int selectedIndex)
        {
            try
            {
                _selectedVoiceIndex = selectedIndex;
                selectVoice(selectedIndex).HandleExceptions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during select voice event: {ex.Message}");
            }
        }
        public static async Task selectVoice(int selectedIndex)
        {
            try
            {

                TagBank tagBank;
                if (!_voices.TryGetValue(selectedIndex, out tagBank))
                {
                    TagBank result = await Singleton<PlayerVoiceLoader>.Instance.TakeVoice(_voiceTemplates[_selectedVoiceIndex].Value.Name, EPhraseTrigger.OnMutter);
                    _voices.Add(selectedIndex, result);
                    if (result == null)
                    {
                        Console.WriteLine($"Voice not available for index: {selectedIndex}");
                        return;
                    }
                }
                string key = _voiceTemplates[_selectedVoiceIndex].Value.Id;

                PatchConstants.BackEndSession.Profile.Customization[EBodyModelPart.Voice] = key;


                int num = global::UnityEngine.Random.Range(0, _voices[selectedIndex].Clips.Length);
                TaggedClip taggedClip = _voices[selectedIndex].Clips[num];
#pragma warning disable CS4014
                Singleton<GUISounds>.Instance.ForcePlaySound(taggedClip.Clip);
#pragma warning restore CS4014

                WTTChangeVoice(key);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during select voice: {ex.Message}");
            }
        }

    }
}

#endif

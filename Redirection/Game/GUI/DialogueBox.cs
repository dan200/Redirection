using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Game.Input;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.GUI
{
    public class DialogueBox : DialogBox
    {
        private const int NUM_LINES = 3;
        private const float TYPING_SPEED = 30.0f;

        public static DialogueBox Create(Screen screen)
        {
            // Calculate dimensions
            var font = UIFonts.Smallest;
            float width = screen.Width - 64.0f;
            float height =
                TOP_MARGIN_SIZE +
                (NUM_LINES * font.Height) +
                BOTTOM_MARGIN_SIZE;

            // Create dialog
            var dialog = new DialogueBox(font, width, height);
            return dialog;
        }

        private enum State
        {
            Animating,
            WaitingForMoreText,
            WaitingForContinue,
            Finished
        }

        private Font m_font;
        private bool m_modal;
        private State m_state;

        private string m_characterName;
        private int m_visibleCharacterName;
        private string m_fullDialogue;
        private string[] m_dialogueLines;
        private int m_visibleDialogueLines;
        private int m_visibleDialogueChars;
        private int m_firstLine;
        private float m_typeTimer;

        private Image m_image;
        private Text[] m_lines;
        private InputPrompt m_continuePrompt;

        public string CharacterName
        {
            get
            {
                return m_characterName;
            }
            set
            {
                if (m_characterName != value)
                {
                    m_characterName = value;
                    m_visibleCharacterName = 0;
                    Title = "";

                    m_state = State.Animating;
                    m_typeTimer = 0.0f;
                    UpdatePrompt();
                }
            }
        }

        public ITexture CharacterImage
        {
            get
            {
                return m_image.Texture;
            }
            set
            {
                m_image.Texture = value;
            }
        }

        public string Dialogue
        {
            get
            {
                return m_fullDialogue;
            }
            set
            {
                m_fullDialogue = value;
                m_dialogueLines = SplitDialogue(m_fullDialogue);
                m_visibleDialogueLines = 0;
                m_visibleDialogueChars = 0;
                m_firstLine = 0;
                RequestRebuild();

                m_state = State.Animating;
                m_typeTimer = 0.0f;
                UpdatePrompt();
            }
        }

        public bool Modal
        {
            get
            {
                return m_modal;
            }
            set
            {
                if (m_modal != value)
                {
                    m_modal = value;
                    BlockInput = m_modal;
                    if (m_modal)
                    {
                        if (m_state == State.WaitingForContinue)
                        {
                            m_state = State.Finished;
                        }
                        else if (m_state == State.WaitingForMoreText)
                        {
                            m_state = State.Animating;
                            m_firstLine++;
                            m_typeTimer = 0.0f;
                        }
                    }
                    UpdatePrompt();
                }
            }
        }

        public bool ReadyForInput
        {
            get
            {
                return m_state == State.WaitingForContinue;
            }
        }

        public bool ContinueRequested
        {
            get
            {
                return m_state == State.Finished;
            }
        }

        private DialogueBox(Font font, float width, float height)
            : base("", Anchor.TopMiddle, -0.5f * width, 16.0f, width, height)
        {
            m_font = font;
            m_modal = false;

            PauseWorld = false;
            BlockInput = m_modal;
            FadeWorld = false;
            AllowUserClose = false;
            ShowAcceptPrompt = false;

            m_state = State.Finished;
            m_characterName = "";
            m_visibleCharacterName = 0;
            m_fullDialogue = "";
            m_dialogueLines = new string[0];
            m_visibleDialogueLines = 0;
            m_visibleDialogueChars = 0;
            m_typeTimer = 0.0f;
            m_firstLine = 0;

            // Create elements
            // Populate dialog
            // Image
            var imageWidth = height - BOTTOM_MARGIN_SIZE - BOTTOM_MARGIN_SIZE;
            var imageBorder = new Box(Core.Render.Texture.Get("gui/inset_border.png", true), imageWidth, imageWidth);
            imageBorder.Anchor = Anchor;
            imageBorder.LocalPosition = new Vector2(LocalPosition.X + width - RIGHT_MARGIN_SIZE - 6.0f - imageBorder.Width, LocalPosition.Y + BOTTOM_MARGIN_SIZE);

            m_image = new Image(Core.Render.Texture.Black, imageBorder.Width - 6.0f, imageBorder.Height - 6.0f);
            m_image.Anchor = imageBorder.Anchor;
            m_image.LocalPosition = imageBorder.LocalPosition + new Vector2(3.0f, 3.0f);

            var titleCover = new Image(Core.Render.Texture.Get("gui/dialog_title_cover.png", true), new Quad(0.0f, 0.0f, 0.5f, 1.0f), 32.0f, 32.0f);
            titleCover.Anchor = Anchor;
            titleCover.LocalPosition = new Vector2(imageBorder.LocalPosition.X - 3.0f - 27.0f, LocalPosition.Y);

            var titleCover2 = new Image(Core.Render.Texture.Get("gui/dialog_title_cover.png", true), new Quad(0.5f, 0.0f, 0.5f, 1.0f), 32.0f, 32.0f);
            titleCover2.Anchor = Anchor;
            titleCover2.LocalPosition = new Vector2(titleCover.LocalPosition.X + titleCover.Width, titleCover.LocalPosition.Y);
            titleCover2.Width = (LocalPosition.X + Width - 16.0f) - titleCover2.LocalPosition.X;
            titleCover2.Stretch = true;

            Elements.Add(titleCover);
            Elements.Add(titleCover2);
            Elements.Add(imageBorder);
            Elements.Add(m_image);

            // Text
            float xPos = LocalPosition.X + LEFT_MARGIN_SIZE + 8.0f;
            float yPos = LocalPosition.Y + TOP_MARGIN_SIZE;
            m_lines = new Text[NUM_LINES];
            for (int i = 0; i < m_lines.Length; ++i)
            {
                var line = new Text(font, "", UIColours.Text, TextAlignment.Left);
                line.Anchor = Anchor;
                line.LocalPosition = new Vector2(xPos, yPos);
                m_lines[i] = line;
                Elements.Add(line);
                yPos += line.Height;
            }

            // Prompts
            m_continuePrompt = new InputPrompt(UIFonts.Smaller, "", TextAlignment.Right);
            m_continuePrompt.Anchor = Anchor;
            m_continuePrompt.LocalPosition = LocalPosition + new Vector2(Width, Height + 0.5f * BOTTOM_MARGIN_SIZE);
            m_continuePrompt.Key = Key.Return;
            m_continuePrompt.GamepadButton = GamepadButton.A;
            m_continuePrompt.SteamControllerButton = SteamControllerButton.MenuSelect;
            m_continuePrompt.MouseButton = MouseButton.Left;
            m_continuePrompt.OnClick += delegate
            {
                TryContinue();
            };
            Elements.Add(m_continuePrompt);
        }

        protected override void OnInit()
        {
            base.OnInit();
            m_continuePrompt.String = Screen.Language.Translate("menus.continue");
            UpdatePrompt();
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if (IsOpen)
            {
                switch (m_state)
                {
                    case State.Animating:
                        {
                            if (CheckContinue())
                            {
                                // Skip animation
                                Title = m_characterName;
                                m_visibleCharacterName = m_characterName.Length;
                                if (m_modal)
                                {
                                    m_visibleDialogueLines = Math.Min(m_dialogueLines.Length, m_firstLine + NUM_LINES);
                                    m_visibleDialogueChars = 0;
                                    if (m_visibleDialogueLines < m_dialogueLines.Length)
                                    {
                                        m_state = State.WaitingForMoreText;
                                    }
                                    else
                                    {
                                        m_state = State.WaitingForContinue;
                                    }
                                }
                                else
                                {
                                    m_visibleDialogueLines = m_dialogueLines.Length;
                                    m_visibleDialogueChars = 0;
                                    m_firstLine = Math.Max(m_dialogueLines.Length - NUM_LINES, 0);
                                    m_state = State.Finished;
                                }
                                RequestRebuild();
                                UpdatePrompt();
                            }
                            else
                            {
                                // Update animation
                                var charTime = 1.0f / TYPING_SPEED;
                                m_typeTimer -= dt;
                                while (m_typeTimer <= 0.0f)
                                {
                                    if (m_visibleCharacterName < m_characterName.Length)
                                    {
                                        m_visibleCharacterName += Font.AdvanceGlyph(m_characterName, m_visibleCharacterName, true);
                                        Title = m_characterName.Substring(0, m_visibleCharacterName);
                                        m_typeTimer += charTime;
                                    }
                                    else if (m_visibleDialogueLines < m_dialogueLines.Length)
                                    {
                                        var currentLine = m_dialogueLines[m_visibleDialogueLines];
                                        m_visibleDialogueChars += Font.AdvanceGlyph(currentLine, m_visibleDialogueChars, true);
                                        if (m_visibleDialogueChars >= currentLine.Length)
                                        {
                                            m_visibleDialogueLines++;
                                            m_visibleDialogueChars = 0;
                                            if (m_visibleDialogueLines < m_dialogueLines.Length &&
                                                m_visibleDialogueLines >= m_firstLine + NUM_LINES)
                                            {
                                                if (m_modal)
                                                {
                                                    m_state = State.WaitingForMoreText;
                                                    UpdatePrompt();
                                                }
                                                else
                                                {
                                                    m_firstLine++;
                                                }
                                            }
                                        }
                                        RequestRebuild();
                                        m_typeTimer += charTime;
                                    }
                                    else
                                    {
                                        m_state = (m_modal ? State.WaitingForContinue : State.Finished);
                                        UpdatePrompt();
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    case State.WaitingForMoreText:
                        {
                            // Check for continue
                            if (CheckContinue())
                            {
                                TryContinue();
                            }
                            break;
                        }
                    case State.WaitingForContinue:
                        {
                            // Check for continue
                            if (CheckContinue())
                            {
                                TryContinue();
                            }
                            break;
                        }
                }
            }
        }

        private bool CheckContinue()
        {
            if (Screen.CheckSelect())
            {
                return true;
            }
            else if (Screen.Mouse.Buttons[MouseButton.Left].Pressed)
            {
                Screen.InputMethod = InputMethod.Mouse;
                return true;
            }
            return false;
        }

        private void TryContinue()
        {
            if (m_state == State.WaitingForContinue)
            {
                m_state = State.Finished;
                UpdatePrompt();
            }
            else if (m_state == State.WaitingForMoreText)
            {
                m_state = State.Animating;
                m_typeTimer = 0.0f;
                m_firstLine = m_visibleDialogueLines;
                UpdatePrompt();
            }
        }

        private void UpdatePrompt()
        {
            m_continuePrompt.Visible =
                (m_state == State.WaitingForContinue) ||
                (m_state == State.WaitingForMoreText);
        }

        private static int EmitDialoguePage(string dialogue, int start, int linesPerPage, Font font, float maxWidth, List<string> o_results)
        {
            // Count the number of lines needed to emit the dialogue
            int endPos = start;
            int lines = 0;
            while (endPos < dialogue.Length)
            {
                endPos += font.WordWrap(dialogue, endPos, true, maxWidth);
                endPos += Font.AdvanceWhitespace(dialogue, endPos);
                if ((++lines) == linesPerPage)
                {
                    break;
                }
            }

            if (endPos < dialogue.Length)
            {
                // Clip to the end of the previous sentence
                int sentenceStart = start;
                while (sentenceStart < endPos)
                {
                    int sentenceEnd = sentenceStart + Font.AdvanceSentence(dialogue, sentenceStart);
                    if (sentenceEnd > endPos)
                    {
                        break;
                    }
                    else
                    {
                        sentenceStart = sentenceEnd + Font.AdvanceWhitespace(dialogue, sentenceEnd);
                    }
                }
                if (sentenceStart > start)
                {
                    endPos = sentenceStart;
                }
            }

            // Emit the lines
            int pos = start;
            int line = 0;
            while (pos < endPos && line < linesPerPage)
            {
                int lineLen = font.WordWrap(dialogue, pos, (endPos - pos), true, maxWidth);
                o_results.Add(dialogue.Substring(pos, lineLen));
                pos += lineLen;
                pos += Font.AdvanceWhitespace(dialogue, pos);
                ++line;
            }
            while (line < linesPerPage)
            {
                o_results.Add("");
                ++line;
            }
            return (endPos - start);
        }

        // Split a whole string into lines, without letting sentences cross page boundaries
        private string[] SplitDialogue(string dialogue)
        {
            var maxWidth = Width - LEFT_MARGIN_SIZE - RIGHT_MARGIN_SIZE - 6.0f - (m_image.Width + 6.0f) - 16.0f;
            var results = new List<string>();
            var parts = dialogue.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; ++i)
            {
                var part = parts[i];
                int pos = 0;
                while (pos < part.Length)
                {
                    pos += EmitDialoguePage(part, pos, NUM_LINES, m_font, maxWidth, results);
                }
            }
            return results.ToArray();
        }

        protected override void OnRebuild()
        {
            base.OnRebuild();

            // Update dialogue
            for (int i = 0; i < m_lines.Length; ++i)
            {
                var line = m_lines[i];
                var lineNum = m_firstLine + i;
                if (lineNum < m_visibleDialogueLines)
                {
                    line.String = m_dialogueLines[lineNum];
                }
                else if (lineNum == m_visibleDialogueLines && m_visibleDialogueLines < m_dialogueLines.Length)
                {
                    line.String = m_dialogueLines[lineNum].Substring(0, m_visibleDialogueChars);
                }
                else
                {
                    line.String = "";
                }
            }
        }
    }
}


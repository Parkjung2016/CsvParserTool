using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CSVParserTool
{
    internal sealed class ToolInfoForm : Form
    {
        private sealed class GuideSection
        {
            public string Title { get; }
            public Action Render { get; }

            public GuideSection(string title, Action render)
            {
                Title = title;
                Render = render;
            }
        }

        private sealed class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.UserPaint, true);
                DoubleBuffered = true;
            }
        }

        private sealed class BufferedFlowLayoutPanel : FlowLayoutPanel
        {
            public BufferedFlowLayoutPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.UserPaint, true);
                DoubleBuffered = true;
            }
        }

        private readonly Panel header = new Panel();
        private readonly Label title = new Label();
        private readonly Label subtitle = new Label();
        private readonly Button closeButton = new Button();
        private readonly FlowLayoutPanel navigation = new FlowLayoutPanel();
        private readonly Panel contentHost = new BufferedPanel();
        private readonly FlowLayoutPanel content = new BufferedFlowLayoutPanel();
        private readonly List<Button> navigationButtons = new List<Button>();
        private readonly List<GuideSection> sections = new List<GuideSection>();
        private readonly Font headingFont = new Font("Segoe UI Semibold", 18F, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font subheadingFont = new Font("Segoe UI Semibold", 11F, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font bodyFont = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font bodyMediumFont = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font noteFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private int selectedSectionIndex;
        private System.Windows.Forms.Timer entranceAnimationTimer;
        private System.Windows.Forms.Timer contentAnimationTimer;
        private Point entranceTargetLocation;
        private int entranceAnimationFrame;
        private int contentAnimationFrame;
        private Panel contentTransitionOverlay;
        private int contentTransitionInitialWidth;
        private bool hasBeenShown;

        public ToolInfoForm()
        {
            InitializeSections();
            InitializeLayout();
            ApplyTheme();
            SelectSection(0);
            if (AnimationsEnabled)
                Opacity = 0D;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                entranceAnimationTimer?.Dispose();
                StopContentAnimation();
                contentAnimationTimer?.Dispose();
                headingFont.Dispose();
                subheadingFont.Dispose();
                bodyFont.Dispose();
                bodyMediumFont.Dispose();
                noteFont.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeLayout()
        {
            Text = "PJDev Data Tool 사용 안내";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(860, 800);
            MinimumSize = new Size(720, 540);
            KeyPreview = true;
            Shown += ToolInfoForm_Shown;
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    Close();
            };

            header.Dock = DockStyle.Top;
            header.Height = 82;
            header.Padding = new Padding(24, 15, 18, 12);

            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.Text = "닫기";
            closeButton.Size = new Size(72, 34);
            closeButton.Location = new Point(770, 15);
            closeButton.Margin = Padding.Empty;
            header.Resize += (_, __) => closeButton.Left = header.ClientSize.Width - header.Padding.Right - closeButton.Width;
            closeButton.Click += (_, __) => Close();

            title.AutoSize = true;
            title.Location = new Point(24, 15);
            title.Text = "Data Tool 사용 안내";

            subtitle.AutoSize = true;
            subtitle.Location = new Point(25, 47);
            subtitle.Text = "테이블 작성부터 참조와 Export 검증까지";

            header.Controls.Add(closeButton);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            navigation.Dock = DockStyle.Left;
            navigation.Width = 190;
            navigation.FlowDirection = FlowDirection.TopDown;
            navigation.WrapContents = false;
            navigation.Padding = new Padding(14, 18, 14, 14);
            navigation.AutoScroll = true;

            for (int i = 0; i < sections.Count; i++)
            {
                int index = i;
                var button = new Button
                {
                    Text = sections[i].Title,
                    Width = 160,
                    Height = 40,
                    Margin = new Padding(0, 0, 0, 7),
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                button.Click += (_, __) => SelectSection(index);
                navigationButtons.Add(button);
                navigation.Controls.Add(button);
            }

            contentHost.Dock = DockStyle.Fill;
            contentHost.Padding = new Padding(28, 24, 28, 22);

            content.Dock = DockStyle.Fill;
            content.AutoScroll = true;
            content.FlowDirection = FlowDirection.TopDown;
            content.WrapContents = false;
            content.Padding = Padding.Empty;
            content.TabStop = false;
            content.SizeChanged += (_, __) => ResizeContentChildren();
            contentHost.Controls.Add(content);

            Controls.Add(contentHost);
            Controls.Add(navigation);
            Controls.Add(header);
        }

        private void InitializeSections()
        {
            sections.Add(new GuideSection("시작하기", RenderGettingStarted));
            sections.Add(new GuideSection("파일 · 폴더", RenderFiles));
            sections.Add(new GuideSection("테이블 구조", RenderTableLayout));
            sections.Add(new GuideSection("타입 규칙", RenderTypes));
            sections.Add(new GuideSection("Enum 관리", RenderEnumCatalog));
            sections.Add(new GuideSection("테이블 참조", RenderReferences));
            sections.Add(new GuideSection("버전 · Export", RenderExport));
            sections.Add(new GuideSection("오류 · 검증", RenderValidation));
        }

        private void ApplyTheme()
        {
            BackColor = UiTheme.AppBackground;
            ForeColor = UiTheme.TextPrimary;
            Font = UiTheme.FontUi;

            header.BackColor = UiTheme.HeaderBackground;
            title.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Regular, GraphicsUnit.Point);
            title.ForeColor = UiTheme.TextPrimary;
            subtitle.Font = UiTheme.FontSubtitle;
            subtitle.ForeColor = UiTheme.TextMuted;

            navigation.BackColor = UiTheme.SurfaceMuted;
            contentHost.BackColor = UiTheme.Surface;
            content.BackColor = UiTheme.Surface;
            content.ForeColor = UiTheme.TextPrimary;

            UiTheme.StyleSecondaryButton(closeButton);
            closeButton.MinimumSize = new Size(72, 32);

            foreach (Button button in navigationButtons)
            {
                button.Font = UiTheme.FontUiMedium;
                button.FlatAppearance.BorderSize = 0;
                button.Padding = new Padding(12, 0, 8, 0);
            }
        }

        private void SelectSection(int index)
        {
            if (index < 0 || index >= sections.Count)
                return;

            bool animateContent = hasBeenShown && index != selectedSectionIndex && AnimationsEnabled;
            StopContentAnimation();

            selectedSectionIndex = index;
            for (int i = 0; i < navigationButtons.Count; i++)
            {
                bool selected = i == selectedSectionIndex;
                navigationButtons[i].BackColor = selected ? UiTheme.Accent : UiTheme.SurfaceMuted;
                navigationButtons[i].ForeColor = selected ? UiTheme.TextOnAccent : UiTheme.TextSecondary;
            }

            content.SuspendLayout();
            while (content.Controls.Count > 0)
            {
                Control old = content.Controls[0];
                content.Controls.RemoveAt(0);
                old.Dispose();
            }

            content.AutoScrollPosition = Point.Empty;
            sections[index].Render();
            ResizeContentChildren();
            content.ResumeLayout(true);

            if (animateContent)
                StartContentAnimation();
        }
        private static bool AnimationsEnabled => SystemInformation.IsMenuAnimationEnabled && UiTheme.CurrentTheme == AppTheme.Default;

        private void ToolInfoForm_Shown(object sender, EventArgs e)
        {
            hasBeenShown = true;
            if (!AnimationsEnabled)
            {
                Opacity = 1D;
                return;
            }

            entranceTargetLocation = Location;
            Location = new Point(Location.X, Location.Y + 12);
            entranceAnimationFrame = 0;
            if (entranceAnimationTimer == null)
            {
                entranceAnimationTimer = new System.Windows.Forms.Timer { Interval = 15 };
                entranceAnimationTimer.Tick += (_, __) => AdvanceEntranceAnimation();
            }
            entranceAnimationTimer.Start();
        }

        private void AdvanceEntranceAnimation()
        {
            entranceAnimationFrame++;
            double progress = Math.Min(1D, entranceAnimationFrame / 12D);
            double eased = 1D - Math.Pow(1D - progress, 3D);
            Opacity = eased;
            Location = new Point(
                entranceTargetLocation.X,
                entranceTargetLocation.Y + (int)Math.Round(12D * (1D - eased)));
            if (progress >= 1D)
            {
                entranceAnimationTimer.Stop();
                Opacity = 1D;
                Location = entranceTargetLocation;
            }
        }

        private void StartContentAnimation()
        {
            contentAnimationFrame = 0;
            contentTransitionInitialWidth = Math.Max(1, content.Width);
            contentTransitionOverlay = new BufferedPanel
            {
                Bounds = content.Bounds,
                BackColor = UiTheme.Surface,
                TabStop = false
            };
            contentTransitionOverlay.Controls.Add(new Panel
            {
                Dock = DockStyle.Right,
                Width = 3,
                BackColor = UiTheme.Accent
            });
            contentHost.Controls.Add(contentTransitionOverlay);
            contentTransitionOverlay.BringToFront();

            if (contentAnimationTimer == null)
            {
                contentAnimationTimer = new System.Windows.Forms.Timer { Interval = 15 };
                contentAnimationTimer.Tick += (_, __) => AdvanceContentAnimation();
            }
            contentAnimationTimer.Start();
        }

        private void AdvanceContentAnimation()
        {
            contentAnimationFrame++;
            double progress = Math.Min(1D, contentAnimationFrame / 8D);
            double eased = 1D - Math.Pow(1D - progress, 3D);
            if (contentTransitionOverlay != null)
            {
                int width = Math.Max(0, (int)Math.Round(contentTransitionInitialWidth * (1D - eased)));
                contentTransitionOverlay.Width = width;
            }

            if (progress >= 1D)
                StopContentAnimation();
        }

        private void StopContentAnimation()
        {
            contentAnimationTimer?.Stop();
            if (contentTransitionOverlay != null)
            {
                contentHost.Controls.Remove(contentTransitionOverlay);
                contentTransitionOverlay.Dispose();
                contentTransitionOverlay = null;
            }
            contentTransitionInitialWidth = 0;
        }
        private void RenderGettingStarted()
        {
            Heading("빠르게 시작하기");
            Body("XLSX 원본 폴더와 Unity 프로젝트 경로를 선택한 뒤 Export 버전을 입력하고 ‘데이터 Export’를 누릅니다.");
            Subheading("기본 흐름");
            Step("1", "새 XLSX 버튼으로 테이블을 만들거나 기존 DT_*.xlsx를 준비합니다.");
            Step("2", "첫 세 행에 헤더, 버전, 타입을 입력합니다.");
            Step("3", "데이터를 작성하고 미리보기에서 생성 코드를 확인합니다.");
            Step("4", "Export를 실행해 게임에서 사용할 데이터 파일과 C# 코드를 생성합니다.");
            Note("새 XLSX 이름에 DT_를 입력하지 않아도 자동으로 붙습니다. CharacterStat 입력 → DT_CharacterStat.xlsx 생성");
            Note("위쪽 테마 버튼에서 기본·초원·바다 테마를 고를 수 있습니다. 다크 모드는 선택한 테마에 맞춰 적용됩니다.");
        }

        private void RenderFiles()
        {
            Heading("파일 이름과 폴더");
            Subheading("새 XLSX 만들기");
            Body("이름 입력 칸에는 테이블 이름만 입력해도 됩니다. 확장자는 제거되고 DT_가 없으면 자동으로 붙습니다.");
            Table(
                "입력	실제 생성 파일",
                "CharacterStat	DT_CharacterStat.xlsx",
                "DT_Item	DT_Item.xlsx",
                "Stage.xlsx	DT_Stage.xlsx");
            Bullet("파일명에 사용할 수 없는 문자는 자동으로 제거됩니다.");
            Bullet("같은 이름의 XLSX가 이미 있으면 새로 만들지 않습니다.");
            Bullet("엑셀 파일에 시트가 여러 개 있어도 첫 번째 시트만 변환합니다.");
            Subheading("경로 선택");
            Bullet("프로젝트 경로는 Assets 폴더가 아니라 Unity 프로젝트 루트를 선택합니다.");
            Bullet("XLSX 원본은 작업용 엑셀 파일을 모아 둔 폴더입니다.");
            Bullet("~$로 시작하는 Excel 임시 파일은 목록과 Export에서 제외됩니다.");
            Subheading("원본이 삭제된 테이블의 이전 파일 정리");
            Body("예전에 DT_Item.xlsx를 Export한 뒤 원본 XLSX를 삭제하면, 지난번에 만든 DT_Item의 CSV·게임용 데이터·C# 파일이 남을 수 있습니다. 이 옵션을 켜면 그런 이전 파일을 함께 삭제합니다.");
            Note("이전 출력 파일을 직접 보관하고 싶다면 ‘원본 XLSX가 없는 이전 출력 파일 삭제’ 옵션을 끄세요.");
        }

        private void RenderTableLayout()
        {
            Heading("테이블 기본 구조");
            Body("첫 행은 변수명, 둘째 행은 컬럼 버전, 셋째 행은 타입입니다. 실제 데이터는 넷째 행부터 작성합니다.");
            Table(
                "#설명\tId\tName\tSpeed",
                "버전\t1.0.0\t1.0.0\t1.0.0",
                "타입\tint\tstring\tfloat",
                "\t1\tWarrior\t7.5");
            Subheading("컬럼 규칙");
            Bullet("Id 컬럼은 필수이며 테이블 안에서 고유해야 합니다.");
            Bullet("헤더가 #으로 시작하는 컬럼은 메모용이며 Export에서 제외됩니다.");
            Bullet("헤더명은 생성되는 C# 프로퍼티 이름으로 사용됩니다.");
            Bullet("C# 변수 이름에 쓸 수 없는 공백이나 기호는 자동으로 _로 바뀝니다.");
            Bullet("빈 타입이나 지원하지 않는 타입이 있으면 Export가 실패합니다.");
        }

        private void RenderTypes()
        {
            Heading("지원 타입");
            Body("타입 행에는 아래 타입 중 하나를 입력합니다. 대소문자는 구분하지 않습니다.");
            Bullet("타입에 str이라고 써도 string으로 처리합니다.");
            Code("bool   uint   int   float   double   string\nenum:CharacterState   enum:ItemTag[]\nint[]   string[]\nref CharacterStat.Speed   ref CharacterStat.Speed[]");
            Subheading("Enum");
            Bullet("모든 enum은 Enum XLSX 버튼으로 만든 DT_Enums.xlsx 한 곳에서 관리합니다.");
            Bullet("테이블의 타입 행에는 반드시 enum:CharacterType처럼 DT_Enums.xlsx에 등록한 EnumName을 입력합니다.");
            Bullet("배열도 같은 규칙으로 enum:CharacterType[]처럼 입력합니다. enum 단독 입력이나 E 접두사 방식은 지원하지 않습니다.");
            Subheading("배열");
            Body("여러 값을 넣을 때는 타입 뒤에 []를 붙이고, 한 셀 안의 값은 | 문자로 구분합니다.");
            Table(
                "헤더\t타입\t데이터 입력",
                "Rewards\tint[]\t10|20|30",
                "Tags\tenum:ItemTag[]\tWeapon|Rare",
                "Names\tstring[]\tSword|Shield");
            Bullet("빈 셀은 길이가 0인 빈 배열로 생성됩니다.");
            Bullet("생성되는 C# 타입은 List<T>가 아니라 변경할 필요가 없는 T[] 배열입니다.");
            Subheading("참조 타입");
            Bullet("참조 컬럼은 타입 대신 ref 테이블명.컬럼명 형식으로 입력합니다.");
            Bullet("참조 대상 컬럼의 실제 타입을 자동으로 따라갑니다.");
            Bullet("여러 Id를 참조하려면 ref CharacterStat.Speed[]로 쓰고 데이터에는 1|2|3처럼 입력합니다.");
            Bullet("가져온 값이 또 다른 테이블을 참조해도 마지막 값까지 자동으로 따라갑니다.");
            Bullet("Id 컬럼 자체는 ref 타입으로 지정할 수 없습니다.");
            Note("배열 값 안에 | 문자를 데이터 자체로 넣는 형식은 지원하지 않습니다.");
        }

        private void RenderEnumCatalog()
        {
            Heading("Enum을 한 파일에서 관리하기");
            Body("테이블 목록 위의 Enum XLSX 버튼을 누르면 DT_Enums.xlsx가 생성되고 바로 열립니다. 이미 파일이 있으면 새로 만들지 않고 기존 파일을 엽니다.");
            Step("1", "Enum XLSX 버튼을 누릅니다.");
            Step("2", "EnumName에 타입 이름을, Value에 enum 값을 입력합니다.");
            Step("3", "같은 EnumName을 다음 행에 반복해서 값을 계속 추가합니다.");
            Table(
                "EnumName\tValue\t#설명",
                "CharacterType\tWarrior\t전사",
                "CharacterType\tMage\t마법사",
                "ItemGrade\tCommon\t일반");
            Subheading("생성 결과");
            Code("public enum CharacterType\n{\n    Warrior,\n    Mage,\n}\n\npublic enum ItemGrade\n{\n    Common,\n}");
            Subheading("테이블에서 사용");
            Table(
                "헤더\t타입\t데이터 입력",
                "Type\tenum:CharacterType\tWarrior",
                "AllowedTypes\tenum:CharacterType[]\tWarrior|Mage");
            Bullet("DT_Enums.xlsx에 적힌 enum은 테이블에서 사용하지 않아도 모두 생성됩니다.");
            Bullet("등록되지 않은 enum 값을 테이블에 입력하면 Export가 실패합니다.");
            Bullet("EnumName이나 Value가 중복되거나 C# 이름으로 사용할 수 없으면 해당 XLSX 행을 알려줍니다.");
            Note("#설명은 작업자가 알아보기 위한 메모이며 생성되는 enum 코드에는 포함되지 않습니다.");
        }

        private void RenderReferences()
        {
            Heading("다른 테이블 값 참조하기");
            Body("헤더에는 생성할 변수명을, 타입 행에는 참조 대상을, 데이터 행에는 대상 테이블의 Id를 입력합니다.");
            Table(
                "#설명\tId\tMoveSpeed",
                "버전\t1.0.0\t1.0.0",
                "타입\tint\tref CharacterStat.Speed",
                "\t0\t1");
            Subheading("위 예제의 의미");
            Bullet("MoveSpeed는 DT_CharacterStat에서 Id가 1인 행을 찾습니다.");
            Bullet("해당 행의 Speed 값을 가져와 실제 Export 값으로 사용합니다.");
            Bullet("MoveSpeed의 C# 타입도 CharacterStat.Speed 타입을 따라갑니다.");
            Code("/// <summary>CharacterStat.Speed 참조</summary>\npublic float MoveSpeed { get; set; }");
            Subheading("테이블 이름");
            Body("CharacterStat, DT_CharacterStat, CharacterStatData는 같은 테이블 이름으로 인식합니다.");
        }

        private void RenderExport()
        {
            Heading("버전과 Export");
            Bullet("전체 Export: 목록에 있는 모든 테이블을 생성합니다.");
            Bullet("선택 Export: 테이블 목록 왼쪽에서 체크한 항목만 생성합니다. 참조 대상은 자동으로 함께 확인합니다.");
            Body("각 컬럼의 버전이 현재 Export 버전보다 작거나 같을 때만 결과에 포함됩니다.");
            Bullet("Id 컬럼은 컬럼 버전과 관계없이 항상 Export에 포함됩니다.");
            Table(
                "컬럼 버전\tExport 결과",
                "1.0.0\t포함",
                "1.2.0\t포함",
                "2.0.0\t제외");
            Subheading("생성 결과");
            Bullet("Content/CSV: 사람이 열어서 확인할 수 있는 데이터 파일");
            Bullet("Content/Bytes: 게임에서 빠르게 읽기 위한 데이터 파일");
            Bullet("Scripts: Unity 코드에서 데이터를 읽고 사용하기 위한 C# 파일");
            Note("참조 대상 컬럼이 현재 Export 버전에서 제외되면 참조 오류로 처리됩니다.");
        }

        private void RenderValidation()
        {
            Heading("Export 실패 조건");
            Body("모든 테이블과 참조를 먼저 확인합니다. 하나라도 잘못되면 잘못된 데이터 파일을 만들지 않고 Export를 중단합니다.");
            Bullet("참조 테이블 또는 참조 컬럼이 없음");
            Bullet("입력한 참조 Id가 대상 테이블에 없음");
            Bullet("대상 테이블에 같은 Id가 두 개 이상 있음");
            Bullet("참조가 서로 순환함");
            Bullet("필수 Id, 버전 또는 타입이 잘못됨");
            Subheading("오류 메시지 예시");
            Code("DT_Stage[Id=0].MoveSpeed:\nDT_CharacterStat에서 Id '999'을 찾을 수 없습니다.\n(Speed 참조)");
            Note("오류에는 테이블, 행 Id, 컬럼명이 함께 표시되므로 해당 셀을 바로 수정할 수 있습니다.");
        }

        private void Heading(string text)
        {
            AddText(text, headingFont, UiTheme.TextPrimary, new Padding(0, 0, 0, 14));
        }

        private void Subheading(string text)
        {
            AddText(text, subheadingFont, UiTheme.TextPrimary, new Padding(0, 18, 0, 8));
        }

        private void Body(string text)
        {
            AddText(text, bodyFont, UiTheme.TextSecondary, new Padding(0, 0, 0, 10));
        }

        private void Bullet(string text)
        {
            Label label = CreateWrappedLabel("•  " + text, bodyFont, UiTheme.TextSecondary);
            label.Padding = new Padding(12, 0, 0, 0);
            AddFullWidth(label, new Padding(0, 0, 0, 6));
        }

        private void Step(string number, string text)
        {
            int width = GetContentWidth();
            var row = new TableLayoutPanel
            {
                Name = "GuideStep",
                ColumnCount = 2,
                RowCount = 1,
                Width = width,
                Height = 36,
                BackColor = UiTheme.Surface,
                Margin = new Padding(0, 0, 0, 8)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var badge = new Label
            {
                Text = number,
                Size = new Size(28, 28),
                Margin = new Padding(0, 2, 10, 2),
                BackColor = UiTheme.Accent,
                ForeColor = UiTheme.TextOnAccent,
                Font = bodyMediumFont,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label description = CreateWrappedLabel(text, bodyFont, UiTheme.TextSecondary);
            description.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            description.Margin = new Padding(0, 5, 0, 2);

            row.Controls.Add(badge, 0, 0);
            row.Controls.Add(description, 1, 0);
            content.Controls.Add(row);
            ResizeStructuredRow(row);
        }

        private void Table(params string[] rows)
        {
            if (rows == null || rows.Length == 0)
                return;

            string[] headers = SplitTableRow(rows[0]);
            var grid = new DataGridView
            {
                Name = "GuideTable",
                Width = GetContentWidth(),
                Height = 40 + Math.Max(0, rows.Length - 1) * 32,
                Margin = new Padding(0, 8, 0, 12),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                ColumnHeadersVisible = true,
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = UiTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = UiTheme.Border,
                ScrollBars = ScrollBars.None,
                TabStop = false,
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Accent;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextOnAccent;
            grid.ColumnHeadersDefaultCellStyle.Font = bodyMediumFont;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 6, 0);
            grid.DefaultCellStyle.BackColor = UiTheme.Surface;
            grid.DefaultCellStyle.ForeColor = UiTheme.TextPrimary;
            grid.DefaultCellStyle.Font = bodyFont;
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 6, 0);
            grid.DefaultCellStyle.SelectionBackColor = UiTheme.Surface;
            grid.DefaultCellStyle.SelectionForeColor = UiTheme.TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceMuted;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = UiTheme.SurfaceMuted;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = UiTheme.TextPrimary;
            grid.RowTemplate.Height = 32;

            for (int column = 0; column < headers.Length; column++)
            {
                var gridColumn = new DataGridViewTextBoxColumn
                {
                    Name = "GuideColumn" + column,
                    HeaderText = headers[column],
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    MinimumWidth = column == 0 ? 76 : 100,
                    AutoSizeMode = column == 0
                        ? DataGridViewAutoSizeColumnMode.None
                        : DataGridViewAutoSizeColumnMode.Fill
                };
                if (column == 0)
                    gridColumn.Width = 92;
                grid.Columns.Add(gridColumn);
            }

            for (int row = 1; row < rows.Length; row++)
            {
                string[] cells = SplitTableRow(rows[row]);
                var values = new object[headers.Length];
                for (int column = 0; column < values.Length; column++)
                    values[column] = column < cells.Length ? cells[column] : string.Empty;
                grid.Rows.Add(values);
            }

            grid.SelectionChanged += (_, __) => grid.ClearSelection();
            grid.ClearSelection();
            AddFullWidth(grid, grid.Margin);
        }

        private static string[] SplitTableRow(string row) =>
            (row ?? string.Empty).Split(new[] { '	' }, StringSplitOptions.None);

        private void Code(string text)
        {
            int lineCount = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n').Length;
            var card = new Panel
            {
                Name = "GuideCode",
                Width = GetContentWidth(),
                Height = Math.Max(58, lineCount * 20 + 24),
                Padding = new Padding(12, 10, 12, 10),
                BackColor = UiTheme.SurfaceMuted,
                Margin = new Padding(0, 8, 0, 12)
            };

            var code = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Text = text ?? string.Empty,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = UiTheme.SurfaceMuted,
                ForeColor = UiTheme.IsDarkMode
                    ? Color.FromArgb(191, 219, 254)
                    : Color.FromArgb(30, 64, 175),
                Font = UiTheme.FontMonoFallback,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.None,
                TabStop = false
            };
            card.Controls.Add(code);
            AddFullWidth(card, card.Margin);
        }

        private void Note(string text)
        {
            int width = GetContentWidth();
            var card = new TableLayoutPanel
            {
                Name = "GuideNote",
                ColumnCount = 2,
                RowCount = 1,
                Width = width,
                Height = 52,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = UiTheme.SurfaceMuted,
                Margin = new Padding(0, 10, 0, 8)
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var tip = new Label
            {
                Text = "TIP",
                AutoSize = true,
                Font = UiTheme.FontUiMedium,
                ForeColor = UiTheme.Accent,
                Margin = new Padding(0, 2, 8, 0)
            };
            Label description = CreateWrappedLabel(text, noteFont, UiTheme.TextSecondary);
            description.Margin = new Padding(0, 2, 0, 0);

            card.Controls.Add(tip, 0, 0);
            card.Controls.Add(description, 1, 0);
            content.Controls.Add(card);
            ResizeStructuredRow(card);
        }

        private void AddText(string text, Font font, Color color, Padding margin)
        {
            Label label = CreateWrappedLabel(text, font, color);
            AddFullWidth(label, margin);
        }

        private Label CreateWrappedLabel(string text, Font font, Color color)
        {
            int width = GetContentWidth();
            return new Label
            {
                Text = text ?? string.Empty,
                AutoSize = true,
                MaximumSize = new Size(width, 0),
                Font = font,
                ForeColor = color,
                BackColor = Color.Transparent
            };
        }

        private void AddFullWidth(Control control, Padding margin)
        {
            control.Width = GetContentWidth();
            control.Margin = margin;
            control.Tag = "GuideFullWidth";
            content.Controls.Add(control);
        }

        private int GetContentWidth()
        {
            int width = content.ClientSize.Width
                - content.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth
                - 6;
            return Math.Max(320, width);
        }

        private void ResizeContentChildren()
        {
            if (content.IsDisposed)
                return;

            int width = GetContentWidth();
            foreach (Control control in content.Controls)
            {
                if (!string.Equals(control.Tag as string, "GuideFullWidth", StringComparison.Ordinal)
                    && control.Name != "GuideStep"
                    && control.Name != "GuideNote")
                {
                    continue;
                }

                control.Width = width;
                if (control is Label label)
                    label.MaximumSize = new Size(width - label.Padding.Horizontal, 0);

                if (control is TableLayoutPanel structured)
                    ResizeStructuredRow(structured);
            }
        }

        private void ResizeStructuredRow(TableLayoutPanel row)
        {
            int textColumn = row.Controls.Count > 1 ? 1 : -1;
            if (textColumn < 0 || !(row.Controls[textColumn] is Label description))
                return;

            int available = Math.Max(120, row.Width - row.Padding.Horizontal - 58);
            description.MaximumSize = new Size(available, 0);
            Size preferred = description.GetPreferredSize(new Size(available, 0));
            int verticalPadding = row.Name == "GuideNote" ? row.Padding.Vertical : 4;
            row.Height = Math.Max(row.Name == "GuideNote" ? 48 : 34, preferred.Height + verticalPadding);
        }
    }
}

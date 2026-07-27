using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CSVParserTool.Exporting;

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

        private readonly ExportPlatform exportPlatform;
        private readonly Panel header = new Panel();
        private readonly Label title = new Label();
        private readonly Label subtitle = new Label();
        private readonly Button closeButton = new Button();
        private readonly FlowLayoutPanel navigation = new FlowLayoutPanel();
        private readonly Panel contentHost = new BufferedPanel();
        private readonly MascotPresenter mascot = new MascotPresenter();
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

        public ToolInfoForm(ExportPlatform exportPlatform)
        {
            this.exportPlatform = exportPlatform;
            InitializeSections();
            InitializeLayout();
            ApplyTheme();
            SelectSection(0);
            if (AnimationsEnabled)
                Opacity = 0D;
        }

        public ToolInfoForm() : this(ExportPlatform.Unity)
        {
        }

        private bool IsUnreal => exportPlatform == ExportPlatform.Unreal;
        private string EngineName => IsUnreal ? "Unreal Engine" : "Unity";

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
            subtitle.Text = EngineName + " · 테이블 작성부터 코드 사용과 Export까지";

            header.Controls.Add(closeButton);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            navigation.Dock = DockStyle.Left;
            navigation.Width = 190;
            navigation.FlowDirection = FlowDirection.TopDown;
            navigation.WrapContents = false;
            navigation.Padding = new Padding(14, 18, 14, 14);
            navigation.AutoScroll = true;

            mascot.Size = new Size(156, 150);
            mascot.Margin = new Padding(3, 0, 0, 12);
            mascot.AccessibleName = "Data Tool 사용 안내 마스코트";
            navigation.Controls.Add(mascot);
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
            sections.Add(new GuideSection("코드 사용", RenderCodeUsage));
            sections.Add(new GuideSection("CLI", RenderCli));
            sections.Add(new GuideSection("오류 · 검증", RenderValidation));
        }

        private void ApplyTheme()
        {
            BackColor = UITheme.AppBackground;
            ForeColor = UITheme.TextPrimary;
            Font = UITheme.FontUI;

            header.BackColor = UITheme.HeaderBackground;
            title.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Regular, GraphicsUnit.Point);
            title.ForeColor = UITheme.TextPrimary;
            subtitle.Font = UITheme.FontSubtitle;
            subtitle.ForeColor = UITheme.TextMuted;

            navigation.BackColor = UITheme.SurfaceMuted;
            mascot.BackColor = Color.Transparent;
            contentHost.BackColor = UITheme.Surface;
            content.BackColor = UITheme.Surface;
            content.ForeColor = UITheme.TextPrimary;

            UITheme.StyleSecondaryButton(closeButton);
            closeButton.MinimumSize = new Size(72, 32);

            foreach (Button button in navigationButtons)
            {
                button.Font = UITheme.FontUIMedium;
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
            SetMascotForSection(index);
            for (int i = 0; i < navigationButtons.Count; i++)
            {
                bool selected = i == selectedSectionIndex;
                navigationButtons[i].BackColor = selected ? UITheme.Accent : UITheme.SurfaceMuted;
                navigationButtons[i].ForeColor = selected ? UITheme.TextOnAccent : UITheme.TextSecondary;
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
        private void SetMascotForSection(int index)
        {
            switch (index)
            {
                case 0:
                    mascot.SetSequence(MascotPose.Hello, MascotPose.Point);
                    break;
                case 4:
                case 6:
                    mascot.SetSequence(MascotPose.Celebrate, MascotPose.Hello);
                    break;
                case 1:
                case 2:
                case 7:
                    mascot.SetSequence(MascotPose.Read, MascotPose.Point);
                    break;
                default:
                    mascot.SetSequence(MascotPose.Point, MascotPose.Read);
                    break;
            }
        }
        private static bool AnimationsEnabled => SystemInformation.IsMenuAnimationEnabled;

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
                BackColor = UITheme.Surface,
                TabStop = false
            };
            contentTransitionOverlay.Controls.Add(new Panel
            {
                Dock = DockStyle.Right,
                Width = 3,
                BackColor = UITheme.Accent
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
            Heading(EngineName + " 빠르게 시작하기");
            Body("XLSX 원본 폴더와 " + EngineName + " 프로젝트 루트를 선택한 뒤 Export 버전을 입력하고 ‘데이터 Export’를 누릅니다.");
            Subheading("기본 흐름");
            Step("1", "새 XLSX 버튼으로 테이블을 만들거나 기존 DT_*.xlsx를 준비합니다.");
            Step("2", "첫 세 행에 헤더, 버전, 타입을 입력합니다.");
            Step("3", "데이터를 작성하고 미리보기에서 " + (IsUnreal ? "C++" : "C#") + " 생성 코드를 확인합니다.");
            Step("4", IsUnreal
                ? "Unreal Editor를 닫고 Export하여 C++ 코드와 UDataTable을 생성합니다."
                : "Export하여 CSV·Bytes와 Unity 런타임 C# 코드를 생성합니다.");
            Note("새 XLSX 이름에 DT_를 입력하지 않아도 자동으로 붙습니다. CharacterStat 입력 → DT_CharacterStat.xlsx 생성");
            Note("엔진 선택은 저장됩니다. 위쪽 엔진 버튼에서 바꿀 수 있고, 엔진마다 프로젝트 경로와 XLSX 경로를 따로 기억합니다.");
        }
        private void RenderFiles()
        {
            Heading(EngineName + " 파일과 폴더");
            Subheading("새 XLSX 만들기");
            Body("이름 입력 칸에는 테이블 이름만 입력해도 됩니다. 확장자는 제거되고 DT_가 없으면 자동으로 붙습니다.");
            Table(
                "입력\t실제 생성 파일",
                "CharacterStat\tDT_CharacterStat.xlsx",
                "DT_Item\tDT_Item.xlsx",
                "Stage.xlsx\tDT_Stage.xlsx");
            Bullet("파일명에 사용할 수 없는 문자는 자동으로 제거됩니다.");
            Bullet("같은 이름의 XLSX가 이미 있으면 새로 만들지 않습니다.");
            Bullet("~$로 시작하는 Excel 임시 파일은 목록과 Export에서 제외됩니다.");
            Subheading("프로젝트 경로");
            if (IsUnreal)
            {
                Bullet(".uproject 파일이 하나 있는 Unreal 프로젝트 루트를 선택합니다.");
                Bullet("C++ 코드: Source/{Module}/DataTables/Generated (헤더와 cpp를 한 폴더에서 관리)");
                Bullet("UDataTable: Content/PJDevData/DataTables");
                Note("중간 CSV·JSON은 프로젝트에 남기지 않습니다. Content Browser에는 최종 UDataTable만 표시됩니다.");
            }
            else
            {
                Bullet("Assets 폴더 자체가 아니라 Assets와 ProjectSettings가 있는 Unity 프로젝트 루트를 선택합니다.");
                Bullet("C# 코드: Assets/_Game/DataTables/Scripts");
                Bullet("CSV·Bytes: Assets/_Game/DataTables/Content");
                Note("‘원본 없는 테이블 산출물 정리’를 켜면 XLSX가 사라진 테이블의 생성 파일도 Export 때 정리합니다.");
            }
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
            Bullet("Id 컬럼은 필수이며 각 테이블 안에서 고유해야 합니다.");
            Bullet("헤더가 #으로 시작하는 컬럼은 메모용이며 Export에서 제외됩니다.");
            Bullet("헤더명은 생성되는 " + (IsUnreal ? "USTRUCT 필드" : "C# 프로퍼티") + " 이름으로 사용됩니다.");
            Bullet("앞뒤 공백은 제거하고, 코드 식별자에 쓸 수 없는 공백이나 기호는 _로 바꿉니다.");
            Bullet("빈 타입이나 지원하지 않는 타입이 있으면 Export가 실패합니다.");
        }
        private void RenderTypes()
        {
            Heading("지원 타입");
            Body("타입 행에는 아래 타입 중 하나를 입력합니다. 기본 타입 이름은 대소문자를 구분하지 않습니다.");
            Code("bool   uint   int   float   double   string\nenum:CharacterState   enum:ItemTag[]\nint[]   string[]\nref CharacterStat.Speed   ref CharacterStat.Speed[]\nkeyref Stat.Id   keyref Stat.Id[]");
            Subheading("Enum");
            Bullet("모든 enum은 Enum XLSX 버튼으로 만든 DT_Enums.xlsx 한 곳에서 관리합니다.");
            Bullet("타입 행에는 enum:CharacterType, 배열은 enum:CharacterType[] 형식으로 입력합니다.");
            Bullet(IsUnreal
                ? "생성 시 CharacterType은 Unreal 규칙에 맞는 ECharacterType UENUM으로 바뀝니다."
                : "생성 시 CharacterType C# enum을 그대로 사용합니다.");
            Subheading("배열");
            Body("타입 뒤에 []를 붙이고 한 셀의 여러 값은 | 문자로 구분합니다.");
            Table(
                "헤더\t타입\t데이터 입력",
                "Rewards\tint[]\t10|20|30",
                "Tags\tenum:ItemTag[]\tWeapon|Rare",
                "Names\tstring[]\tSword|Shield");
            Bullet("빈 셀은 길이가 0인 빈 배열로 생성됩니다.");
            Bullet(IsUnreal ? "생성 타입은 TArray<T>입니다." : "생성 타입은 List<T>가 아닌 T[]입니다.");
            Subheading("참조 타입");
            Bullet("ref 테이블명.컬럼명은 대상 값을 가져오고 대상 컬럼의 실제 타입을 따라갑니다.");
            Bullet("keyref 테이블명.컬럼명은 입력값을 유지하면서 대상 값의 존재만 검사합니다.");
            Bullet("배열 참조는 뒤에 []를 붙이고 1|2|3처럼 입력합니다.");
        }
        private void RenderEnumCatalog()
        {
            Heading("Enum을 한 파일에서 관리하기");
            Body("Enum XLSX 버튼을 누르면 비어 있는 DT_Enums.xlsx가 생성됩니다. EnumName과 Value를 한 행씩 추가합니다.");
            Table(
                "EnumName\tValue\t#설명",
                "CharacterType\tWarrior\t전사",
                "CharacterType\tMage\t마법사",
                "ItemGrade\tCommon\t일반");
            Subheading(EngineName + " 생성 결과");
            Code(IsUnreal
                ? "UENUM(BlueprintType)\nenum class ECharacterType : uint8\n{\n    Warrior,\n    Mage,\n};"
                : "public enum CharacterType\n{\n    Warrior,\n    Mage,\n}");
            Subheading("테이블에서 사용");
            Table(
                "헤더\t타입\t데이터 입력",
                "Type\tenum:CharacterType\tWarrior",
                "AllowedTypes\tenum:CharacterType[]\tWarrior|Mage");
            Bullet("DT_Enums.xlsx에 적힌 enum은 테이블에서 사용하지 않아도 모두 생성됩니다.");
            Bullet("등록되지 않은 enum 값이나 완전히 중복된 EnumName·Value가 있으면 Export가 실패합니다.");
            if (IsUnreal)
                Bullet("Unreal 리플렉션 충돌을 막기 위해 ETest와 EtEST처럼 대소문자만 다른 이름도 실패 처리합니다.");
            else
                Bullet("Unity C#에서는 Test와 tEST를 서로 다른 식별자로 유지합니다.");
            Note("#설명은 작업자가 알아보기 위한 메모이며 생성 코드에는 포함되지 않습니다.");
        }
        private void RenderReferences()
        {
            Heading("다른 테이블 참조하기");
            Body("참조는 컬럼 이름으로 추측하지 않고 타입 행의 ref 또는 keyref 규칙으로만 판단합니다.");
            Subheading("실제 값 가져오기 — ref");
            Table(
                "#설명\tId\tMoveSpeed",
                "버전\t1.0.0\t1.0.0",
                "타입\tint\tref CharacterStat.Speed",
                "\t0\t1");
            Bullet("MoveSpeed의 1은 DT_CharacterStat에서 Id가 1인 행의 Speed 값으로 바뀝니다.");
            Bullet("생성 변수 타입도 CharacterStat.Speed의 실제 타입을 자동으로 따라갑니다.");
            Code(IsUnreal
                ? "/** CharacterStat.Speed 참조 */\nUPROPERTY(EditAnywhere, BlueprintReadOnly)\nfloat MoveSpeed = 0.0f;"
                : "/// <summary>CharacterStat.Speed 참조</summary>\npublic float MoveSpeed { get; set; }");
            Subheading("값은 유지하고 존재만 확인 — keyref");
            Table(
                "#설명\tId\tCharacterId\tStatId\tBaseValue",
                "버전\t1.0.0\t1.0.0\t1.0.0\t1.0.0",
                "타입\tint\tint\tkeyref Stat.Id\tfloat",
                "\t10000\t1000\tHealth\t100");
            Bullet("Health는 그대로 Export되며 DT_Stat.Id에 Health가 있는지만 확인합니다.");
            Bullet("대상 값이 없거나 참조가 순환하면 Preview와 Export가 실패합니다.");
            Bullet("배열은 keyref Stat.Id[]와 Health|Attack 형식으로 입력합니다.");
            Note("ref는 값을 가져오고, keyref는 입력값을 유지한 채 존재 여부만 확인합니다.");
        }
        private void RenderExport()
        {
            Heading(EngineName + " 버전과 Export");
            Bullet("전체 Export는 모든 테이블을, 선택 Export는 목록 왼쪽 체크 영역에서 선택한 테이블만 생성합니다.");
            Bullet("선택하지 않은 테이블도 참조 검증에는 함께 사용됩니다.");
            Body("컬럼 버전이 현재 Export 버전보다 작거나 같을 때만 포함되며 Id는 항상 포함됩니다.");
            Table(
                "컬럼 버전\tExport 1.2.0 결과",
                "1.0.0\t포함",
                "1.2.0\t포함",
                "2.0.0\t제외");
            if (IsUnreal)
            {
                Subheading("Unreal 생성 결과");
                Bullet("Source/{Module}/DataTables/Generated 한 폴더에 UENUM·USTRUCT 헤더와 cpp를 생성합니다.");
                Bullet("GlobalDataStorage와 InfoStorage 프레임워크를 생성하고 Editor 타깃을 컴파일합니다.");
                Bullet("XLSX 데이터를 메모리에서 UDataTable로 변환하여 /Game/PJDevData/DataTables에 저장합니다.");
                Bullet("프로젝트에는 중간 CSV나 JSON을 남기지 않습니다.");
                Bullet("Unreal Editor가 열려 있으면 미저장 작업 보호를 위해 강제 종료하지 않고 Export를 중단합니다.");
                Note("C++ 헤더는 IDE 또는 C++ Classes에서, 생성된 UDataTable은 Content Browser에서 확인합니다.");
            }
            else
            {
                Subheading("Unity 생성 결과");
                Bullet("Content/CSV: 사람이 확인할 수 있는 최종 데이터");
                Bullet("Content/Bytes: MessagePack 기반 런타임 데이터");
                Bullet("Scripts: 데이터 클래스, Container, Loader, InfoStorage C# 코드");
                Bullet("GlobalDataContainer.LoadAllAsync()가 원본 테이블과 등록한 InfoStorage를 순서대로 로드합니다.");
                Note("UniTask가 없으면 동기 API인 LoadAll()이 생성됩니다.");
            }
            Bullet("하단 '모두 정리' 버튼은 현재 엔진에서 Data Tool이 만든 코드와 데이터 폴더를 확인 후 한 번에 삭제합니다. XLSX 원본은 유지됩니다.");
            Note("참조 대상 컬럼이 현재 Export 버전에서 제외되면 참조 오류로 처리됩니다.");
        }
        private void RenderCodeUsage()
        {
            Heading(EngineName + " 코드에서 사용하기");
            if (IsUnreal)
            {
                Subheading("원본 테이블 조회");
                Body("GlobalDataStorage는 GameInstance Subsystem이라 별도 생성이나 초기화 호출이 필요 없습니다.");
                Code("const UGlobalDataStorage* Data = UGlobalDataStorage::Get(this);\nconst FStatDefinitionRow* Row =\n    Data ? Data->FindStatDefinition(TEXT(\"1\")) : nullptr;");
                Subheading("가공 데이터 — InfoStorage");
                Body("여러 테이블 조합이나 검색용 Map은 생성 폴더 밖에 사용자 InfoStorage로 작성합니다.");
                Code("class FGameStatInfoStorage final : public IInfoStorage\n{\npublic:\n    void Build(const UGlobalDataStorage& Data) override\n    {\n        Data.GetAllStatDefinition(Rows);\n    }\nprivate:\n    TArray<FStatDefinitionRow> Rows;\n};\n\n// .cpp에서 한 번만 등록\nREGISTER_INFO_STORAGE(FGameStatInfoStorage);");
                Code("const FGameStatInfoStorage* Stats =\n    FInfoStorageRegistry::Get<FGameStatInfoStorage>();");
                Note("등록된 InfoStorage의 Build는 원본 UDataTable 로드 직후 자동 호출됩니다. 사용자 파일은 Export가 덮어쓰지 않습니다.");
            }
            else
            {
                Subheading("전체 데이터 로드");
                Code("using PJDev.Data;\n\nawait GlobalDataContainer.Instance.LoadAllAsync();");
                Subheading("Id로 한 행 조회");
                Code("StatDefinitionData row =\n    GlobalDataContainer.Instance.GetStatDefinitionData(1);\n\nif (GlobalDataContainer.Instance.TryGetStatDefinitionData(1, out var found))\n{\n    // found 사용\n}");
                Subheading("가공 데이터 — InfoStorage");
                Code("var stats = new GameStatInfoStorage(\n    GlobalDataContainer.Instance.StatDefinitionData);\nInfoStorageRegistry.Register(stats);\n\nawait GlobalDataContainer.Instance.LoadAllAsync();\nGameStatInfoStorage loaded = InfoStorageRegistry.Get<GameStatInfoStorage>();");
                Note("InfoStorage는 LoadAllAsync() 전에 한 번 등록합니다. UniTask가 없는 프로젝트에서는 LoadAll()을 사용합니다.");
            }
        }

        private void RenderCli()
        {
            Heading(EngineName + " CLI 사용");
            Body("GUI와 같은 검증·생성 파이프라인을 명령줄이나 빌드 자동화에서 실행합니다.");
            Subheading("기본 명령");
            Code(IsUnreal
                ? "DataTool.exe export --engine unreal --project \"D:\\Game\\MyUnrealProject\" --excel \"D:\\Data\\Xlsx\" --refresh-xlsx --version 1.0.0"
                : "DataTool.exe export --engine unity --project \"D:\\Game\\MyUnityProject\" --excel \"D:\\Data\\Xlsx\" --refresh-xlsx --version 1.0.0");
            Subheading("주요 옵션");
            Table(
                "옵션\t설명",
                "--project <경로>\t프로젝트 루트 (필수)",
                "--engine unity|unreal\tExport 엔진 (생략 시 unity)",
                "--excel <경로>\tDT_*.xlsx 원본 폴더",
                "--refresh-xlsx\tXLSX 원본 검사와 산출물 정리 실행",
                "--version <버전>\t예: 1.0.0; 생략 시 모든 컬럼",
                "--no-orphan-cleanup\t원본 없는 기존 산출물 유지");
            if (IsUnreal)
                Bullet("--no-unreal-import를 추가하면 C++ 코드만 생성하고 컴파일·UDataTable 자동 Import를 생략합니다.");
            Bullet("성공 종료 코드는 0, 잘못된 옵션·검증·Export 실패는 1입니다.");
            Bullet("DataTool.exe --help로 전체 옵션을 확인할 수 있습니다.");
            Note("전체 CLI 문서는 저장소의 docs/CLI.md에 있습니다.");
        }

        private void RenderValidation()
        {
            Heading(EngineName + " Export 실패 조건");
            Body("모든 테이블과 참조를 먼저 검사합니다. 하나라도 잘못되면 Export를 중단합니다.");
            Bullet("ref 또는 keyref 대상 테이블·컬럼·값이 없음");
            Bullet("한 테이블 안에 같은 Id가 두 개 이상 있음");
            Bullet("ref 참조가 서로 순환함");
            Bullet("필수 Id, 버전 또는 타입이 잘못됨");
            Bullet("EnumName·Value가 중복되었거나 등록되지 않은 enum 값을 사용함");
            if (IsUnreal)
                Bullet("Unreal enum 이름 또는 값이 대소문자만 달라 엔진 이름이 충돌함");
            Subheading("오류 메시지 예시");
            Code("DT_CharacterStat[Id=10000].StatId:\nDT_Stat.Id에서 값 'Health'을(를) 찾을 수 없습니다.\n(keyref 존재 검증)");
            Note("오류에는 테이블, 행 Id, 컬럼명이 함께 표시되므로 잘못된 값을 바로 찾을 수 있습니다.");
        }
        private void Heading(string text)
        {
            AddText(text, headingFont, UITheme.TextPrimary, new Padding(0, 0, 0, 14));
        }

        private void Subheading(string text)
        {
            AddText(text, subheadingFont, UITheme.TextPrimary, new Padding(0, 18, 0, 8));
        }

        private void Body(string text)
        {
            AddText(text, bodyFont, UITheme.TextSecondary, new Padding(0, 0, 0, 10));
        }

        private void Bullet(string text)
        {
            Label label = CreateWrappedLabel("•  " + text, bodyFont, UITheme.TextSecondary);
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
                BackColor = UITheme.Surface,
                Margin = new Padding(0, 0, 0, 8)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var badge = new Label
            {
                Text = number,
                Size = new Size(28, 28),
                Margin = new Padding(0, 2, 10, 2),
                BackColor = UITheme.Accent,
                ForeColor = UITheme.TextOnAccent,
                Font = bodyMediumFont,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label description = CreateWrappedLabel(text, bodyFont, UITheme.TextSecondary);
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
                BackgroundColor = UITheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = UITheme.Border,
                ScrollBars = ScrollBars.None,
                TabStop = false,
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = UITheme.Accent;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.TextOnAccent;
            grid.ColumnHeadersDefaultCellStyle.Font = bodyMediumFont;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 6, 0);
            grid.DefaultCellStyle.BackColor = UITheme.Surface;
            grid.DefaultCellStyle.ForeColor = UITheme.TextPrimary;
            grid.DefaultCellStyle.Font = bodyFont;
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 6, 0);
            grid.DefaultCellStyle.SelectionBackColor = UITheme.Surface;
            grid.DefaultCellStyle.SelectionForeColor = UITheme.TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.BackColor = UITheme.SurfaceMuted;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = UITheme.SurfaceMuted;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = UITheme.TextPrimary;
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
                BackColor = UITheme.SurfaceMuted,
                Margin = new Padding(0, 8, 0, 12)
            };

            var code = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Text = text ?? string.Empty,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = UITheme.SurfaceMuted,
                ForeColor = UITheme.IsDarkMode
                    ? Color.FromArgb(191, 219, 254)
                    : Color.FromArgb(30, 64, 175),
                Font = UITheme.FontMonoFallback,
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
                BackColor = UITheme.SurfaceMuted,
                Margin = new Padding(0, 10, 0, 8)
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var tip = new Label
            {
                Text = "TIP",
                AutoSize = true,
                Font = UITheme.FontUIMedium,
                ForeColor = UITheme.Accent,
                Margin = new Padding(0, 2, 8, 0)
            };
            Label description = CreateWrappedLabel(text, noteFont, UITheme.TextSecondary);
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

using Content.Server._Funkystation.StationRecords.Components;
using Content.Server.Power.Components;
using Content.Server.StationRecords.Systems;
using Content.Shared._Funkystation.Pager;
using Content.Shared._Funkystation.Pager.Components;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Funkystation.Pager;

public sealed partial class PagerSystem : SharedPagerSystem
{
    [Dependency] private IGameTiming _timing = null!;
    [Dependency] private IRobustRandom _random = null!;
    [Dependency] private UserInterfaceSystem _ui = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedRingerSystem _ringer = null!;
    [Dependency] private StationRecordsSystem _records = null!;

    private readonly HashSet<int> _assignedNumbers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PagerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PagerComponent, PagerSendPageMessage>(OnSendPage);
        SubscribeLocalEvent<PagerComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent args)
    {
        if (!_records.TryGetRecord<GeneralStationRecord>(args.Key, out var record))
            return;

        if (FindPagerNumberForRecord(record) is not { } number)
            return;
        record.PagerNumber = number;

        if (TryComp<XoRecordManifestComponent>(args.Key.OriginStation, out var manifest) &&
            manifest.Published.TryGetValue(args.Key.Id, out var published))
        {
            manifest.Published[args.Key.Id] = published with { PagerNumber = number };
        }
    }

    private int? FindPagerNumberForRecord(GeneralStationRecord record)
    {
        var query = EntityQueryEnumerator<PagerComponent, TransformComponent>();
        while (query.MoveNext(out _, out var pager, out var xform))
        {
            var parent = xform.ParentUid;
            while (parent.IsValid())
            {
                if (Name(parent) == record.Name)
                {
                    if (pager.Number != -1)
                        return pager.Number;
                }
                parent = Transform(parent).ParentUid;
            }
        }
        return null;
    }

    private void OnBuiOpened(Entity<PagerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is PagerUiKey.Key)
            PushUiState(ent);
    }

    private void PushUiState(Entity<PagerComponent> ent)
    {
        var state = new PagerBoundUserInterfaceState(GetNumber(ent), GetMode(ent), GetCurrentPage(ent));
        _ui.SetUiState(ent.Owner, PagerUiKey.Key, state);
    }

    private void OnMapInit(Entity<PagerComponent> ent, ref MapInitEvent args)
    {
        var existing = GetNumber(ent);
        if (existing != -1)
        {
            _assignedNumbers.Add(existing);
            return;
        }

        SetNumber(ent, GetUnusedNumber());
    }

    public int AssignNumber(Entity<PagerComponent> ent)
    {
        var existing = GetNumber(ent);
        if (existing != -1)
            _assignedNumbers.Remove(existing);

        var number = GetUnusedNumber();
        SetNumber(ent, number);
        return number;
    }

    private int GetUnusedNumber()
    {
        var number = _random.Next(MinNumber, MaxNumber + 1);
        var attempts = 0;
        while (_assignedNumbers.Contains(number) && attempts < 2000)
        {
            number = _random.Next(MinNumber, MaxNumber + 1);
            attempts++;
        }

        _assignedNumbers.Add(number);
        return number;
    }

    private void OnSendPage(Entity<PagerComponent> ent, ref PagerSendPageMessage args)
    {
        if (!IsValidNumber(args.TargetNumber))
            return;

        if (!IsValidCode(args.Code))
            return;

        if (!TryConsumeCooldown(ent, _timing.CurTime))
        {
            Popup.PopupEntity(Loc.GetString("pager-send-too-fast"), ent, args.Actor);
            return;
        }

        var senderXform = Transform(ent);
        if (senderXform.GridUid is not { } senderGrid || !GridHasServer(senderGrid))
        {
            Popup.PopupEntity(Loc.GetString("pager-no-signal"), ent, args.Actor);
            return;
        }

        var senderNumber = GetNumber(ent);

        var code = args.Code?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
            code = null;

        var query = EntityQueryEnumerator<PagerComponent, TransformComponent>();
        while (query.MoveNext(out var recvUid, out var recvPager, out var recvXform))
        {
            Entity<PagerComponent> receiver = (recvUid, recvPager);

            if (GetNumber(receiver) != args.TargetNumber)
                continue;

            if (recvXform.GridUid is not { } recvGrid || !GridHasServer(recvGrid))
                continue;

            DeliverPage(receiver, senderNumber, code);
        }
    }

    private void DeliverPage(Entity<PagerComponent> receiver, int senderNumber, string? code)
    {
        SetCurrentPage(receiver, senderNumber, code, _timing.CurTime);
        PushUiState(receiver);

        switch (GetMode(receiver))
        {
            case PagerMode.Beep:
                if (TryComp<RingerComponent>(receiver, out var ringer))
                {
                    _ringer.RingerPlayRingtone((receiver, ringer));
                }
                else
                {
                    _audio.PlayPvs(GetBeepSound(receiver), receiver);
                    Popup.PopupEntity(Loc.GetString("pager-page-received"), receiver);
                }
                break;
            case PagerMode.Buzz:
                _audio.PlayPvs(GetBuzzSound(receiver), receiver);
                break;
            case PagerMode.Mute:
                break;
        }
    }

    private bool GridHasServer(EntityUid grid)
    {
        var query = EntityQueryEnumerator<PagerServerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            if (!TryComp<ApcPowerReceiverComponent>(uid, out var power) || !power.Powered)
                continue;

            return true;
        }

        return false;
    }
}

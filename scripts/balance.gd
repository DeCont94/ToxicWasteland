extends Node
## ВСЕ ЧИСЛА ПРОТОТИПА — В ЭТОМ ФАЙЛЕ.
## Поменял число, сохранил (Ctrl+S), запустил игру заново (F5).
## Расстояния — в метрах, время — в секундах.

const PX_PER_M := 64.0          # сколько пикселей в одном метре (экран ≈ 20 м в ширину)

# --- Движение Артёма, м/с ---
var speed_sneak := 1.5          # крадучись (Ctrl)
var speed_walk := 3.0           # шаг
var speed_run := 6.0            # бег (Shift)

# --- Шум: радиус волны в метрах ---
var noise_sneak := 2.0          # шаг крадучись
var noise_walk := 5.0           # обычный шаг
var noise_run := 12.0           # шаг бегом
var noise_breath_calm := 1.0    # спокойное дыхание в маске
var noise_breath_heavy := 4.0   # частое дыхание после бега
var noise_glass := 10.0         # стекло под ногой (крадучись — тихо)
var noise_gasp := 10.0          # судорожный вдох, когда воздух в лёгких кончился
var noise_exhale_quiet := 1.0   # отпустил «Замри» вовремя
var noise_exhale_loud := 5.0    # отпустил «Замри» на последних 25%
var noise_cough := 10.0         # кашель (после контакта и при удушье)
var noise_filter_click := 3.0   # щелчок при замене фильтра
var wall_factor := 0.5          # стенка между шумом и симбиотом делит радиус

# --- Как часто звучат шаги и дыхание, с ---
var step_sneak := 0.55
var step_walk := 0.4
var step_run := 0.28
var breath_interval := 1.2

# --- Задержка дыхания (кнопка «Замри» — Пробел) ---
var hold_capacity := 8.0        # полный запас, с
var hold_fear_distance := 4.0   # симбиот ближе этого — запас тает быстрее
var hold_fear_mult := 1.5       # во сколько раз быстрее
var hold_quiet_part := 0.25     # если осталось больше этой доли — выдох тихий
var hold_recover_time := 6.0    # за сколько секунд запас восстанавливается полностью
var winded_after_run := 3.0     # столько секунд бега — и Артём запыхался
var winded_duration := 6.0      # сколько длится одышка (запас задержки вдвое меньше)

# --- Фильтр ---
var filter_full := 240.0        # свежий фильтр, с
var filter_start := 120.0       # сколько осталось в фильтре на старте прототипа
var spares_start := 0           # запасных фильтров на старте
var spares_max := 2             # больше не унести
var drain_heavy := 1.5          # расход при частом дыхании
var drain_run := 2.0            # расход при беге
var filter_change_time := 3.0   # замена фильтра (R), Артём стоит на месте
var suffocation_time := 15.0    # сколько можно продержаться без воздуха
var contact_filter_cost := 60.0 # контакт с симбиотом отнимает столько секунд фильтра
var contact_mode := "filter"    # "filter" — удар по фильтру (утверждено), "fail" — сразу провал

# --- Симбиот ---
var sym_speed_drift := 0.8      # дрейф по маршруту
var sym_speed_alert := 1.5      # идёт к месту шума
var sym_speed_rush := 9.0       # рывок (быстрее бега: убежать нельзя)
var sym_alert_forget := 4.0     # столько тишины — и он успокаивается
var sym_second_noise := 3.0     # второй шум за это время — рывок
var sym_reaction_grace := 0.8   # шумы чаще этого считаются одним (время игроку замереть)
var sym_find_distance := 0.0    # дошёл до места шума, а Артём ближе этого — рывок (0 = выключено: по канону замершего не находят)
var sym_rush_windup := 0.5      # тишина перед рывком
var sym_rush_time := 3.0        # длительность рывка
var sym_contact_distance := 0.7 # касание
var sym_sensitivity := 1.0      # множитель слуха (позже его поднимет тревога роя)
var sym_deaf_after_contact := 3.0

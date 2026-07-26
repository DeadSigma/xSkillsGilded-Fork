import os
import re

# Словарь с файлами и соответствующими им новыми строками перевода
replacements = {
    "zh-CN.json": " - 猫眼 开/关",
    "be.json": " - Кацінае зрок Укл/Выкл",
    "cn.json": " - 猫眼 开/关",
    "cs.json": " - Kočičí vidění Zap/Vyp",
    "de.json": " - Katzensicht Ein/Aus",
    "en.json": " - Cat Eyes On/Off",
    "es-419.json": " - Visión de Gato Activar/Desactivar",
    "es-es.json": " - Visión de Gato Activar/Desactivar",
    "fr.json": " - Vision de Chat Activé/Désactivé",
    "ja.json": " - 猫の目 オン/オフ",
    "pl.json": " - Kocie Oczy Wł./Wył.",
    "pt-br.json": " - Olhos de Gato Lig/Desl",
    "ro.json": " - Vedere de Pisică Pornit/Oprit",
    "ru.json": " - Кошачье зрение Вкл/Выкл",
    "sv.json": " - Kattsyn På/Av",
    "uk.json": " - Котячий зір Увімк./Вимк."
}

DIRECTORY = '.' 

def update_localizations():
    # Регулярное выражение для поиска в сыром тексте файла.
    # Ищет <font... >P</font>, затем любой текст до первого <br>
    pattern = r'(<font[^>]+>\s*P\s*</font>).*?(<br>)'

    for filename, replacement_text in replacements.items():
        filepath = os.path.join(DIRECTORY, filename)
        
        if not os.path.exists(filepath):
            print(f"[ПРОПУСК] Файл {filename} не найден.")
            continue
            
        try:
            # Читаем файл как сырой текст, игнорируя BOM, если он есть
            with open(filepath, 'r', encoding='utf-8-sig') as f:
                content = f.read()
        except Exception as e:
            print(f"[ОШИБКА ЧТЕНИЯ] Файл {filename}: {e}")
            continue
                
        # Производим замену непосредственно в тексте
        new_content = re.sub(pattern, rf'\1{replacement_text}\2', content, flags=re.DOTALL)
        
        if new_content != content:
            try:
                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"[ГОТОВО] Файл {filename} успешно обновлен!")
            except Exception as e:
                print(f"[ОШИБКА ЗАПИСИ] Файл {filename}: {e}")
        else:
            print(f"[БЕЗ ИЗМЕНЕНИЙ] В {filename} не найдена строка для замены.")

if __name__ == "__main__":
    update_localizations()
// JS-инициализатор Razor-библиотеки. Blazor сам загружает этот файл в каждом приложении,
// которое ссылается на ZealousMindedPeopleGeo, поэтому приложению достаточно ссылки на пакет
// и регистрации сервисов: стили библиотеки подключаются здесь.
// Чтобы стили были уже при первой отрисовке, без мелькания, можно подключить их в <head>
// и вручную, тогда второй раз они не добавятся.

const STYLESHEET = '_content/ZealousMindedPeopleGeo/css/zealous-geo.css';

export function ensureStylesheet(doc = document) {
    const href = new URL(STYLESHEET, doc.baseURI).href;
    const links = [...doc.querySelectorAll('link[rel="stylesheet"]')];
    if (links.some((link) => link.href === href)) {
        return;
    }

    const link = doc.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    link.setAttribute('data-zealous-geo', '');

    // После Bootstrap (компоненты уточняют его классы), но до стилей приложения,
    // чтобы приложение могло переопределить оформление библиотеки.
    const bootstrap = links.filter((item) => /bootstrap/i.test(item.href)).pop();
    const anchor = bootstrap ? bootstrap.nextSibling : (links[0] ?? null);
    doc.head.insertBefore(link, anchor);
}

// Blazor Web App (.NET 8+)
export function beforeWebStart() {
    ensureStylesheet();
}

export function afterWebStarted(blazor) {
    // Улучшенная навигация заменяет содержимое <head> страницей с сервера.
    blazor?.addEventListener?.('enhancedload', () => ensureStylesheet());
}

// Blazor Server и Blazor WebAssembly без Blazor Web App
export function beforeStart() {
    ensureStylesheet();
}

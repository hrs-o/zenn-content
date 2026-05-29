---
title: "PHPのstatic呼び出しが判りづらいので再整理"
emoji: "🔖"
type: "tech" # tech: 技術記事 / idea: アイデア
topics: ["php"]
published: true
---

## まえがき

## 3つのstatic

PHPで単にstaticと表現する場合

### 静的メソッドやプロパティを指す

```php

class Hoge {
     // これは静的プロパティのstatic
    private static $foo = 1;


    // これは静的メソッドのstatic
    public static function huga()
    {
        // NOP
    }
}
```







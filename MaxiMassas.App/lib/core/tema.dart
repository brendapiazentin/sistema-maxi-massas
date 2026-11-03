import 'package:flutter/material.dart';

class Tema {
  static const Color vinho = Color(0xFF9E1B0F);

  static ThemeData claro() => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(seedColor: vinho),
        scaffoldBackgroundColor: const Color(0xFFFFFBF5),
        appBarTheme: const AppBarTheme(
          backgroundColor: vinho,
          foregroundColor: Colors.white,
          centerTitle: true,
        ),
      );
}
